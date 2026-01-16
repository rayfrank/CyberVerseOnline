#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace AvocadoShark
{
    public class Hi : EditorWindow
    {
        private Texture2D headerSectionTexture;
        private Vector2 scrollPosition;
        bool ifshowreview = false;

        [InitializeOnLoadMethod]
        private static void Initialize()
        {
#if !CMPSETUP_COMPLETE
            EditorPrefs.SetInt("CMP_StartCount", 0);
#endif
            if (SessionState.GetBool("CMP_ShowedWindow", false)) 
                return;
            if (EditorPrefs.GetBool("CMP_NeverShowAgain", false))
                return;
            var startCount = EditorPrefs.GetInt("CMP_StartCount", 0);

            if (startCount >= 3)
            {
                EditorApplication.delayCall += ShowWindow;
            }

            EditorPrefs.SetInt("CMP_StartCount", ++startCount);

            SessionState.SetBool("CMP_ShowedWindow", true);
        }
        
        [MenuItem("Tools/CMP/Review Asset")]
        public static void ShowWindow()
        {
            EditorApplication.delayCall -= ShowWindow;
            GetWindow(typeof(Hi));
        }
        private void OnEnable()
        {
            InitTextures();
            Repaint();
        }

        private void InitTextures()
        {
            headerSectionTexture = Resources.Load("Green Check") as Texture2D;
        }

        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            GUIStyle Title = new GUIStyle(GUI.skin.label);
            Title.fontSize = 28;
            Title.fontStyle = FontStyle.Bold;
            Title.wordWrap = true;

            GUIStyle SubTitle = new GUIStyle(GUI.skin.label);
            SubTitle.fontSize = 15;
            SubTitle.wordWrap = true;

            int hour = DateTime.Now.Hour;

            GUILayout.Space(20);

            if (hour >= 0 && hour < 12)
            {
                GUILayout.Label("Good Morning!", Title);
            }
            else if (hour >= 12 && hour < 17)
            {
                GUILayout.Label("Good Afternoon!", Title);
            }
            else
            {
                GUILayout.Label("Good Evening!", Title);
            }

            GUILayout.Space(20);

            GUILayout.Label("Thank you for using Clean Multiplayer Pro! If you enjoy the asset, please consider leaving a review:", SubTitle);
            if (GUILayout.Button("Review Asset"))
            {
                Application.OpenURL("https://assetstore.unity.com/packages/slug/264984");
                ifshowreview = true;
            }
            if (ifshowreview)
            {
                EditorGUILayout.HelpBox("Thank you!", MessageType.Info);
            }

            GUILayout.Space(20);
            if (GUILayout.Button("Later"))
            {
                EditorPrefs.SetInt("CMP_StartCount", 0);
                SessionState.SetBool("CMP_ShowedWindow", false);
                EditorPrefs.SetBool("CMP_NeverShowAgain", false);
                this.Close();
            }
            if (GUILayout.Button("Don't show again"))
            {
                EditorPrefs.SetBool("CMP_NeverShowAgain", true);
                this.Close();
            }
            if (GUILayout.Button("If at any time you need help, feel free to ask on the Discord server", EditorStyles.linkLabel))
            {
                Application.OpenURL("https://discord.gg/mP4yfHxXPa");
            }
            if (GUILayout.Button("For more, check out Avocado Shark", EditorStyles.linkLabel))
            {
                Application.OpenURL("https://avocadoshark.com/");
            }
            EditorGUILayout.EndScrollView();
        }
    }
}
#endif
