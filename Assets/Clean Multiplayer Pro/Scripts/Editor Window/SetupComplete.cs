#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System;
using UnityEditor.SceneManagement;

namespace AvocadoShark
{
    public class SetupComplete : EditorWindow
    {
        private Texture2D headerSectionTexture;
        private Vector2 scrollPosition;
        public static void ShowWindow()
        {
            GetWindow(typeof(SetupComplete));
        }
        private void OnEnable()
        {
            InitTextures();
            Repaint();
        }
        void InitTextures()
        {
            headerSectionTexture = Resources.Load("Green Check") as Texture2D;
        }
        void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            Rect textureRect = new Rect(10, 10, headerSectionTexture.width / 16, headerSectionTexture.height / 16); // Reduced dimensions for the texture
            GUI.DrawTexture(textureRect, headerSectionTexture);
            Repaint();
            GUILayout.Space(160);

            GUIStyle Title = new GUIStyle(GUI.skin.label);
            Title.fontSize = 28;
            Title.fontStyle = FontStyle.Bold;
            Title.wordWrap = true;

            GUIStyle SubTitle = new GUIStyle(GUI.skin.label);
            SubTitle.fontSize = 15;
            SubTitle.wordWrap = true;

            GUIStyle greentitle = new GUIStyle(GUI.skin.label);
            greentitle.normal.textColor = Color.green;
            greentitle.hover.textColor = Color.green;
            greentitle.fontSize = 28;
            greentitle.fontStyle = FontStyle.Bold;
            greentitle.wordWrap = true;

            GUILayout.Label("Great Job!", greentitle);
            DateTime justDate = DateTime.Now.Date;
            string dateStr = justDate.ToString("yyyy/MM/dd");
            GUILayout.Label("That's it! You've successfully imported everything!\n\nMark today (" + dateStr + ") as it's the beggining of something BIG", SubTitle);
            GUILayout.Space(20);
            GUILayout.Label("Now, open the menu scene and enjoy", Title);
            if (GUILayout.Button("Open Menu Scene"))
            {
                EditorSceneManager.OpenScene("Assets/Clean Multiplayer Pro/Scenes/Menu.unity");
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
