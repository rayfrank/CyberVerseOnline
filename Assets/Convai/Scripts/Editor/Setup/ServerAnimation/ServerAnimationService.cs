using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Convai.Scripts.Editor.Setup.ServerAnimation {

    internal static class ServerAnimationService {
        private const string DIRECTORY_SAVE_KEY = "CONVAI_SERVER_ANIMATION_SAVE_PATH";

        public static async Task ImportAnimations( List<ServerAnimationItemResponse> animations ) {
            if ( !ConvaiAPIKeySetup.GetAPIKey( out string apiKey ) ) return;
            if ( animations.Count == 0 ) {
                EditorUtility.DisplayDialog( "Import Animation Process", "Cannot start import process since no animations are selected", "Ok" );
                return;
            }

            string savePath = UpdateAnimationSavePath();
            if ( string.IsNullOrEmpty( savePath ) ) {
                EditorUtility.DisplayDialog( "Failed", "Import Operation Cancelled", "Ok" );
                return;
            }

            List<string> allAnimations = animations.Select( x => x.AnimationName ).ToList();
            List<string> successfulImports = new();
            foreach ( ServerAnimationItemResponse anim in animations ) {
                bool result = await ServerAnimationAPI.DownloadAnimation( anim.AnimationID, apiKey, savePath, anim.AnimationName );
                if ( result ) successfulImports.Add( anim.AnimationName );
            }

            LogResult( successfulImports, allAnimations );
            AssetDatabase.Refresh();
        }

        private static void LogResult( List<string> successfulImports, List<string> animPaths ) {
            string dialogMessage = $"Successfully Imported{Environment.NewLine}";
            successfulImports.ForEach( x => dialogMessage += x + Environment.NewLine );
            List<string> unSuccessFullImports = animPaths.Except( successfulImports ).ToList();
            if ( unSuccessFullImports.Count > 0 ) {
                dialogMessage += $"Could not import{Environment.NewLine}";
                unSuccessFullImports.ForEach( x => dialogMessage += x + Environment.NewLine );
            }

            EditorUtility.DisplayDialog( "Import Animation Result", dialogMessage, "Ok" );
        }

        private static string UpdateAnimationSavePath() {
            string selectedPath;
            string currentPath = EditorPrefs.GetString( DIRECTORY_SAVE_KEY, Application.dataPath );
            while ( true ) {
                selectedPath = EditorUtility.OpenFolderPanel( "Select folder within project", currentPath, "" );
                if ( string.IsNullOrEmpty( selectedPath ) ) {
                    selectedPath = string.Empty;
                    break;
                }

                if ( !IsSubfolder( selectedPath, Application.dataPath ) ) {
                    EditorUtility.DisplayDialog( "Invalid Folder Selected", "Please select a folder within the project", "Ok" );
                    continue;
                }

                EditorPrefs.SetString( DIRECTORY_SAVE_KEY, selectedPath );
                break;
            }

            return selectedPath;
        }

        private static bool IsSubfolder( string pathA, string pathB ) {
            // Get full paths to handle any relative path issues
            string fullPathA = Path.GetFullPath( pathA );
            string fullPathB = Path.GetFullPath( pathB );

            // Create URI objects for the paths
            Uri uriA = new(fullPathA);
            Uri uriB = new(fullPathB);

            // Check if pathA is under pathB
            return uriB.IsBaseOf( uriA );
        }
    }

}