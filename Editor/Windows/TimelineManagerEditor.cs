using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;

namespace PixelWizards.Timeline
{
    public class TimelineManager : EditorWindow
    {
        private static List<PlayableDirector> timelineList = new List<PlayableDirector>();
        private static Vector2 windowSize = Vector2.zero;
        private Vector2 scrollPosition;
        private static EditorWindow thisWindow;

        [MenuItem("Window/Sequencing/Timeline Manager")]

        public static void ShowWindow()
        {
            thisWindow = EditorWindow.GetWindow(typeof(TimelineManager));
            
            thisWindow.titleContent = new GUIContent("Timeline Manager");
            windowSize = thisWindow.position.size;
            EditorSceneManager.sceneOpened += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, OpenSceneMode mode)
        {
            UpdatePlayableDirectors();
        }

        void OnGUI()
        {
            // we lose the window handle on domain reloads so grab it again if this happens
            if(thisWindow == null)
            {
                thisWindow = EditorWindow.GetWindow(typeof(TimelineManager));
            }
            // refresh our window size in case we resized it
            windowSize = thisWindow.position.size;

            // The actual window code goes here
            UpdatePlayableDirectors();

            GUILayout.Label("Playable Director shortcuts", EditorStyles.boldLabel);

            scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Width(windowSize.x), GUILayout.Height(windowSize.y - 200));
            {
                GUILayout.BeginVertical();
                {
                    foreach (var timeline in timelineList)
                    {
                        if(GUILayout.Button(timeline.name, GUILayout.ExpandWidth(true), GUILayout.Height(35f)))
                        {
                            Selection.activeObject = timeline.gameObject;
                        }
                    }
                }
                GUILayout.EndVertical();
            }
            GUILayout.EndScrollView();

            if( GUILayout.Button("Refresh", GUILayout.ExpandWidth(true), GUILayout.Height(35f)))
            {
                UpdatePlayableDirectors();
            }
        }

        private void UpdateWindow()
        {
            thisWindow = EditorWindow.GetWindow(typeof(TimelineManager));
            windowSize = thisWindow.position.size;
        }

        private static void UpdatePlayableDirectors()
        {
            var list = Resources.FindObjectsOfTypeAll(typeof(PlayableDirector)) as PlayableDirector[];
            timelineList = list.ToList();
        }
    }
}