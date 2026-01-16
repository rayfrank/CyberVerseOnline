#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

namespace AvocadoShark
{
    [InitializeOnLoad]
    public class SetupWizard : EditorWindow
    {
        static SetupWizard()
        {
#if !CMPSETUP_COMPLETE
        EditorApplication.delayCall += OnInitialize;
#endif
        }

        static void OnInitialize()
        {
            ShowWindow();
        }

        private Texture2D headerSectionTexture;
        private ListRequest request;
        private Vector2 scrollPosition;
        bool showTMPHelpBox = false;
        bool showInputSystemHelpBox = false;
        bool hasAssignedProjectWideInputActions=false;
        bool showVoiceHelpBox = false;
        bool showFusionHelpBox = false;
        bool iscompleteshowing = false;

        [MenuItem("Tools/CMP/Setup Wizard")]
        public static void ShowWindow()
        {
            GetWindow(typeof(SetupWizard));
        }

        private void OnEnable()
        {
            InitTextures();
            request = Client.List(); // List packages currently installed
            EditorApplication.update += Progress;
            Repaint();
            InitTextures();
        }

        private void OnDisable()
        {
            EditorApplication.update -= Progress;
        }

        private void Progress()
        {
            if (request.IsCompleted)
            {
                EditorApplication.update -= Progress;
            }
        }

        void InitTextures()
        {
            headerSectionTexture = Resources.Load("Identity") as Texture2D;
        }

        void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            // Draw the header
            GUILayout.Label(new GUIContent(headerSectionTexture));
            Repaint();
            GUILayout.Space(20);

            GUIStyle Title = new GUIStyle(GUI.skin.label);
            Title.fontSize = 28;
            Title.fontStyle = FontStyle.Bold;
            Title.wordWrap = true;

            GUIStyle SubTitle = new GUIStyle(GUI.skin.label);
            SubTitle.fontSize = 15;
            SubTitle.wordWrap = true;

            GUIStyle checkStyle = new GUIStyle(GUI.skin.label);
            checkStyle.normal.textColor = Color.green;
            checkStyle.hover.textColor = Color.green;
            checkStyle.fontSize = 13;
            checkStyle.wordWrap = true;

            GUILayout.Label("Welcome to Clean Multiplayer Pro!", Title);
            GUILayout.Label(
                "Thank you for choosing Avocado Shark, your professional multiplayer journey is about to begin!",
                SubTitle);
            GUILayout.Space(20);
            GUILayout.Label("Setup", Title);
            GUILayout.Label(
                "There are a few things that need to be done before you can begin using Clean Multiplayer Pro:",
                SubTitle);
            if (GUILayout.Button("Click here to see the Getting Started tutorial", EditorStyles.linkLabel))
            {
                Application.OpenURL("https://youtu.be/uCFWPN8QdFg");
            }

            if (GUILayout.Button("Need Help? Ask on the Discord server", EditorStyles.linkLabel))
            {
                Application.OpenURL("https://discord.gg/mP4yfHxXPa");
            }

            if (GUILayout.Button("For more, check out Avocado Shark", EditorStyles.linkLabel))
            {
                Application.OpenURL("https://avocadoshark.com/");
            }

            GUILayout.Space(20);

            // Color Space Setup
            EditorGUILayout.Space();
            GUILayout.Label("Color Space Setup", EditorStyles.boldLabel);
            if (PlayerSettings.colorSpace != ColorSpace.Linear)
            {
                if (GUILayout.Button("Switch to Linear Color Space"))
                {
                    PlayerSettings.colorSpace = ColorSpace.Linear;
                }
            }
            else
            {
                GUILayout.Label("Done ✔️", checkStyle);
            }

            EditorGUILayout.Space();

            // Scene Setup
            EditorGUILayout.Space();
            GUILayout.Label("Scene Setup", EditorStyles.boldLabel);

            //Scene paths
            string[] scenePathsToAdd = new string[]
            {
                "Assets/Clean Multiplayer Pro/Scenes/Menu.unity",
                "Assets/Clean Multiplayer Pro/Scenes/Game.unity",
                "Assets/Clean Multiplayer Pro/Scenes/Environment 1.unity",
                "Assets/Clean Multiplayer Pro/Scenes/Environment 2.unity",
            };

            bool scenesAreInBuild = AreScenesInBuild(new List<string>(scenePathsToAdd));

            if (!scenesAreInBuild)
            {
                if (GUILayout.Button("Add Scenes to Build"))
                {
                    // Get the current list of scenes in build settings
                    List<EditorBuildSettingsScene> currentScenes =
                        new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);

                    // Add scenes to the build settings (if they aren't there already)

                    for (int i = scenePathsToAdd.Length - 1; i >= 0; i--)

                    {
                        if (!currentScenes.Exists(s => s.path == scenePathsToAdd[i]))

                        {
                            currentScenes.Insert(0, new EditorBuildSettingsScene(scenePathsToAdd[i], true));
                        }
                    }

                    // Update the build settings
                    EditorBuildSettings.scenes = currentScenes.ToArray();
                }
            }
            else
            {
                GUILayout.Label("Done ✔️", checkStyle);
            }

            EditorGUILayout.Space();
            
            bool AreScenesInBuild(List<string> scenePaths)
            {
                List<EditorBuildSettingsScene> currentScenes =
                    new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
                // Check if all scenes exist in the current build settings
                foreach (var scenePath in scenePaths)
                {
                    if (!currentScenes.Exists(s => s.path == scenePath))
                    {
                        return false;
                    }
                }
                return true;
            }
            GUILayout.Space(10);

            GUILayout.Label("Package Management", EditorStyles.boldLabel);

            if (request.IsCompleted)
            {
                if (request.Status == StatusCode.Success)
                {
                    GUI.enabled = !IsFusionInstalled(); // Disable the GUI if Fusion is installed
                    if (GUILayout.Button("Install Fusion 2"))
                    {
                        Application.OpenURL("https://assetstore.unity.com/packages/tools/network/photon-fusion-267958");
                        showFusionHelpBox = true;
                    }

                    if (showFusionHelpBox)
                    {
                        EditorGUILayout.HelpBox(
                            "Make sure to input your 'Photon Fusion 2' App ID in Assets/Photon/Fusion/Resources/PhotonAppSettings.asset",
                            MessageType.Info);
                    }

                    GUI.enabled = true; // Enable the GUI for subsequent controls
                    if (IsFusionInstalled())
                    {
                        GUILayout.Label("Installed ✔️", checkStyle);
                    }

                    GUI.enabled = !IsPhotonVoiceInstalled(); // Disable the GUI if Fusion is installed
                    if (GUILayout.Button("Install Photon Voice 2"))
                    {
                        Application.OpenURL("https://assetstore.unity.com/packages/tools/audio/photon-voice-2-130518");
                        showVoiceHelpBox = true;
                    }

                    if (showVoiceHelpBox)
                    {
                        EditorGUILayout.HelpBox(
                            "Make sure to input your 'Photon Voice' App ID in Assets/Photon/Fusion/Resources/PhotonAppSettings.asset",
                            MessageType.Info);
                    }

                    GUI.enabled = true; // Enable the GUI for subsequent controls
                    if (IsPhotonVoiceInstalled())
                    {
                        GUILayout.Label("Installed ✔️", checkStyle);
                    }

                    GUI.enabled = IsPhotonVoiceInstalled() && HasPun;
                    if (IsPhotonVoiceInstalled() && GUILayout.Button("Remove PUN from Photon Voice"))
                    {
                        RemovePun();
                    }

                    GUI.enabled = true; // Enable the GUI for subsequent controls
                    if (IsPhotonVoiceInstalled() && !HasPun)
                    {
                        GUILayout.Label("Pun Removed ✔️", checkStyle);
                    }

                    GUI.enabled = !IsFusionPhysicsAddOnInstall(); // Disable the GUI if Fusion is installed
                    if (GUILayout.Button("Install Fusion Physics Package"))
                    {
                        Application.OpenURL("https://doc.photonengine.com/fusion/current/addons/physics/download");
                    }

                    GUI.enabled = true; // Enable the GUI for subsequent controls
                    if (IsFusionPhysicsAddOnInstall())
                    {
                        GUILayout.Label("Installed ✔️", checkStyle);
                    }
                    
                    var isInstalled = IsPackageInstalled("com.unity.inputsystem");
                    GUI.enabled = !isInstalled;
                    if (GUILayout.Button("Install InputSystem & Enable it"))
                    {
                        using (new EditorGUI.DisabledScope(isInstalled))
                        {
                            Client.Add("com.unity.inputsystem");
                        }

                        showInputSystemHelpBox = true;
                    }

                    GUI.enabled = true;
                    if (IsPackageInstalled("com.unity.inputsystem"))
                    {
#if ENABLE_INPUT_SYSTEM
                        GUILayout.Label("Installed and Enabled ✔️", checkStyle);
                        showInputSystemHelpBox = false;
#if UNITY_6000_0_OR_NEWER                        
                        GUI.enabled = !hasAssignedProjectWideInputActions;
                        if (GUILayout.Button("Setup Project Wide Input Actions."))
                        {
                            var inputActionAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.InputSystem.InputActionAsset>(
                                "Assets/Clean Multiplayer Pro/Controller/Input System/StarterAssets.inputactions");
                            UnityEngine.InputSystem.InputSystem.actions = inputActionAsset;
                            hasAssignedProjectWideInputActions = true;
                        }

                        GUI.enabled = true;
#endif
                        
                        if(hasAssignedProjectWideInputActions)
                            GUILayout.Label("Done ✔️", checkStyle);

#else
                    showInputSystemHelpBox = true;
                    GUILayout.Label("Installed, but not enabled");
#endif
                    }

                    if (showInputSystemHelpBox)
                    {
                        EditorGUILayout.HelpBox(
                            "To enable the input system: Go to 'Edit' -> 'Project Settings' -> 'Player'. Expand the 'Other Settings' section. Locate the 'Active Input Handling' option. Set it to 'Both'. This should restart your editor",
                            MessageType.Info);
                    }
#if ENABLE_INPUT_SYSTEM
                    showInputSystemHelpBox = false;
#endif
                    bool isTMPinstall = IsTextMeshProInstalled();
                    if (isTMPinstall)
                        GUI.enabled = false;
                    if (GUILayout.Button("Install TextMeshPro"))
                    {
                        showTMPHelpBox = true;
                    }

                    if (showTMPHelpBox)
                    {
                        EditorGUILayout.HelpBox(
                            "Please go to 'Window -> TextMeshPro -> Import TMP Essential Resources' to install it",
                            MessageType.Info);
                    }

                    GUI.enabled = true;
                    if (isTMPinstall)
                    {
                        GUILayout.Label("Installed ✔️", checkStyle);
                        showTMPHelpBox = false;
                    }

                    AddPackageButton("com.unity.cinemachine@2.9.7", "Cinemachine");
                    AddPackageButton("com.unity.postprocessing", "Post Processing");
                }
                else
                {
                    EditorGUILayout.LabelField("Failed to list packages.");
                }
            }
            else
            {
                EditorGUILayout.LabelField("Loading package list...");
            }

            EditorGUILayout.EndScrollView();

            if (request.IsCompleted)
            {
                bool packagesinstalled = AreAllPackagesInstalled();
                if (packagesinstalled)
                {
                    AddDefineSymbols("CMPSETUP_COMPLETE");
                    if (!iscompleteshowing)
                    {
                        SetupComplete.ShowWindow();
                        iscompleteshowing = true;
                    }
                }
            }
        }

        private void AddPackageButton(string packageId, string displayName)
        {
            bool isInstalled = IsPackageInstalled(packageId);
            using (new EditorGUI.DisabledScope(isInstalled))
            {
                if (GUILayout.Button($"Install {displayName}"))
                {
                    Client.Add(packageId);
                }
            }

            if (isInstalled)
            {
                GUIStyle checkStyle = new GUIStyle(GUI.skin.label);
                checkStyle.normal.textColor = Color.green;
                checkStyle.hover.textColor = Color.green;
                checkStyle.fontSize = 13;
                checkStyle.wordWrap = true;
                GUILayout.Label("Installed ✔️", checkStyle);
            }
        }

        private void AddDefineSymbols(string defineSymbol)
        {
            string definesString =
                PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
            List<string> allDefines = definesString.Split(';').ToList();
            if (!allDefines.Contains(defineSymbol))
            {
                allDefines.Add(defineSymbol);
                PlayerSettings.SetScriptingDefineSymbolsForGroup(
                    EditorUserBuildSettings.selectedBuildTargetGroup,
                    string.Join(";", allDefines.ToArray())
                );
            }
        }

        bool IsFusionInstalled()
        {
#if FUSION_WEAVER
            return true;
#else
        return false;
#endif
        }

        private bool IsPhotonVoiceInstalled()
        {
            var result = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name == "PhotonVoice.Fusion");
            return result != null;
        }

        private bool IsFusionPhysicsAddOnInstall()
        {
            var result = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name == "Fusion.Addons.Physics");
            return result != null;
        }

        public static bool HasPun
        {
            get
            {
                return Type.GetType("Photon.Pun.PhotonNetwork, Assembly-CSharp") != null ||
                       Type.GetType("Photon.Pun.PhotonNetwork, Assembly-CSharp-firstpass") != null ||
                       Type.GetType("Photon.Pun.PhotonNetwork, PhotonUnityNetworking") != null;
            }
        }

        bool IsTextMeshProInstalled()
        {
            return Directory.Exists("Assets/TextMesh Pro");
        }

        private bool IsPackageInstalled(string packageId)
        {
            foreach (var package in request.Result)
            {
                if (package.packageId.StartsWith(packageId))
                {
                    return true;
                }
            }

            return false;
        }

        private static void RemovePun()
        {
            DeleteDirectory("Assets/Photon/PhotonVoice/Demos/DemoVoiceProximityChat");
            DeleteDirectory("Assets/Photon/PhotonVoice/Demos/DemoVoicePun");
            DeleteDirectory("Assets/Photon/PhotonVoice/Code/PUN");
            DeleteDirectory("Assets/Photon/PhotonUnityNetworking");
            CleanUpPunDefineSymbols();
            if (EditorUtility.DisplayDialog("AVOCADO SHARK", "Please Restart the editor for proper installation",
                    "Restart"))
            {
                EditorApplication.OpenProject(Directory.GetCurrentDirectory());
            }
        }
        public static void DeleteDirectory(string path)
        {
            if (Directory.Exists(path))
            {
                if (!FileUtil.DeleteFileOrDirectory(path))
                {
                    Debug.LogWarningFormat("Directory \"{0}\" not deleted.", path);
                }
                DeleteFile(string.Concat(path, ".meta"));
            }
            else
            {
                Debug.LogWarningFormat("Directory \"{0}\" does not exist.", path);
            }
        }
        public static void DeleteFile(string path)
        {
            if (File.Exists(path))
            {
                if (!FileUtil.DeleteFileOrDirectory(path))
                {
                    Debug.LogWarningFormat("File \"{0}\" not deleted.", path);
                }
            }
            else
            {
                Debug.LogWarningFormat("File \"{0}\" does not exist.", path);
            }
        }
        public static void CleanUpPunDefineSymbols()
        {
            foreach (BuildTarget target in Enum.GetValues(typeof(BuildTarget)))
            {
                BuildTargetGroup group = BuildPipeline.GetBuildTargetGroup(target);

                if (group == BuildTargetGroup.Unknown)
                {
                    continue;
                }

                var defineSymbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(group)
                    .Split(';')
                    .Select(d => d.Trim())
                    .ToList();

                List<string> newDefineSymbols = new List<string>();
                foreach (var symbol in defineSymbols)
                {
                    if ("PHOTON_UNITY_NETWORKING".Equals(symbol) || symbol.StartsWith("PUN_2_"))
                    {
                        continue;
                    }

                    newDefineSymbols.Add(symbol);
                }

                try
                {
                    PlayerSettings.SetScriptingDefineSymbolsForGroup(group, string.Join(";", newDefineSymbols.ToArray()));
                }
                catch (Exception e)
                {
                    Debug.LogErrorFormat("Could not set clean up PUN2's define symbols for build target: {0} group: {1}, {2}", target, group, e);
                }
            }
        }
        bool AreAllPackagesInstalled()
        {
            var packageIds = new string[]
            {
                "com.unity.cinemachine",
                "com.unity.inputsystem",
#if !UNITY_2023_2_OR_NEWER
                "com.unity.textmeshpro",
#endif
                "com.unity.postprocessing"
            };

            foreach (string packageId in packageIds)
            {
                if (!IsPackageInstalled(packageId))
                {
                    return false; // At least one package isn't installed, so return false
                }
            }

            if (!IsFusionInstalled())
                return false;
            if (!IsPhotonVoiceInstalled())
                return false;
            if (!IsFusionPhysicsAddOnInstall())
                return false;
            if (HasPun)
                return false;
            return true; // If it got through the loop without returning, all packages are installed
        }
    }
}
#endif