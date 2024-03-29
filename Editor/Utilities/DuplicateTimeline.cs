using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
 
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using UnityEngine.SceneManagement;
 
using UnityEditor;

namespace PixelWizards.Timeline
{
    public class DuplicateTimeline : MonoBehaviour
    {
        [MenuItem("Assets/Duplicate Timeline", true)]
        private static bool DupTimelineValidate()
        {
            if (UnityEditor.Selection.activeObject as GameObject == null)
            {
                Debug.Log("Null active object");
                return false;
            }

            GameObject playableDirectorObj = UnityEditor.Selection.activeObject as GameObject;

            PlayableDirector playableDirector = playableDirectorObj.GetComponent<PlayableDirector>();
            if (playableDirector == null)
            {
                Debug.Log("Null playableDirector");
                return false;
            }

            TimelineAsset timelineAsset = playableDirector.playableAsset as TimelineAsset;
            if (timelineAsset == null)
            {
                Debug.Log("Null timelineAsset");
                return false;
            }

            string path = AssetDatabase.GetAssetPath(timelineAsset);
            if (string.IsNullOrEmpty(path))
            {
                Debug.Log("Null timeline asset path");
                return false;
            }

            return true;
        }

        [MenuItem("Assets/Duplicate Timeline")]
        public static void DupTimeline()
        {
            // Fetch playable director, fetch timeline
            GameObject playableDirectorObj = UnityEditor.Selection.activeObject as GameObject;
            PlayableDirector playableDirector = playableDirectorObj.GetComponent<PlayableDirector>();
            TimelineAsset timelineAsset = playableDirector.playableAsset as TimelineAsset;

            // Duplicate
            string path = AssetDatabase.GetAssetPath(timelineAsset);
            string newPath = path.Replace(".", "(Clone).");
            if (!AssetDatabase.CopyAsset(path, newPath))
            {
                Debug.LogError("Couldn't Clone Asset");
                return;
            }

            // Copy Bindings
            TimelineAsset newTimelineAsset = AssetDatabase.LoadMainAssetAtPath(newPath) as TimelineAsset;
            PlayableBinding[] oldBindings = timelineAsset.outputs.ToArray();
            PlayableBinding[] newBindings = newTimelineAsset.outputs.ToArray();
            for (int i = 0; i < oldBindings.Length; i++)
            {
                playableDirector.playableAsset = timelineAsset;
                Object boundTo = playableDirector.GetGenericBinding(oldBindings[i].sourceObject);

                playableDirector.playableAsset = newTimelineAsset;
                playableDirector.SetGenericBinding(newBindings[i].sourceObject, boundTo);
            }

            // Copy Exposed References
            playableDirector.playableAsset = newTimelineAsset;
            foreach (TrackAsset newTrackAsset in newTimelineAsset.GetRootTracks())
            {
                foreach (TimelineClip newClip in newTrackAsset.GetClips())
                {
                    foreach (FieldInfo fieldInfo in newClip.asset.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                    {
                        if (fieldInfo.FieldType.IsGenericType && fieldInfo.FieldType.GetGenericTypeDefinition() == typeof(ExposedReference<>))
                        {
                            // Fetch Old Exposed Name
                            object exposedReference = fieldInfo.GetValue(newClip.asset);
                            PropertyName oldExposedName = (PropertyName)fieldInfo.FieldType
                                .GetField("exposedName")
                                .GetValue(exposedReference);
                            bool isValid;

                            // Fetch Old Exposed Value
                            Object oldExposedValue = playableDirector.GetReferenceValue(oldExposedName, out isValid);
                            if (!isValid)
                            {
                                Debug.LogError("Failed to copy exposed references to duplicate timeline. Could not find: " + oldExposedName);
                                return;
                            }

                            // Replace exposedName on struct
                            PropertyName newExposedName = new PropertyName(UnityEditor.GUID.Generate().ToString());
                            fieldInfo.FieldType
                                .GetField("exposedName")
                                .SetValue(exposedReference, newExposedName);

                            // Set ExposedReference
                            fieldInfo.SetValue(newClip.asset, exposedReference);

                            // Set Reference on Playable Director
                            playableDirector.SetReferenceValue(newExposedName, oldExposedValue);
                        }
                    }
                }
            }
        }
    }
}