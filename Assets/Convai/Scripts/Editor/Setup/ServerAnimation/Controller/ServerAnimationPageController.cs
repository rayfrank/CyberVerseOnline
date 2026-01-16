using Convai.Scripts.Editor.Setup.ServerAnimation.Model;
using Convai.Scripts.Editor.Setup.ServerAnimation.View;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Convai.Scripts.Editor.Setup.ServerAnimation.Controller
{

    internal class ServerAnimationPageController
    {
        private readonly ServerAnimationPageView _ui;


        internal ServerAnimationPageController(ServerAnimationPageView view)
        {
            Data = new ServerAnimationPageData();
            _ui = view;
            if (ConvaiSDKSetupEditorWindow.IsApiKeySet)
            {
                InjectData();
            }
            else
            {
                ConvaiSDKSetupEditorWindow.OnAPIKeySet += () =>
                {
                    if (ConvaiSDKSetupEditorWindow.IsApiKeySet)
                    {
                        InjectData();
                    }
                };
            }
            _ui.RefreshBtn.clicked += RefreshBtnOnClicked;
            _ui.ImportBtn.clicked += ImportBtnOnClicked;
            _ui.NextPageBtn.clicked += NextPageBtnOnClicked;
            _ui.PreviousPageBtn.clicked += PreviousPageBtnOnClicked;
        }


        internal ServerAnimationPageData Data { get; }
        private List<ServerAnimationItemController> Items { get; set; } = new();


        ~ServerAnimationPageController()
        {
            _ui.RefreshBtn.clicked -= RefreshBtnOnClicked;
            _ui.ImportBtn.clicked -= ImportBtnOnClicked;
        }

        private async void ImportBtnOnClicked()
        {
            List<ServerAnimationItemResponse> selectedAnimations = Items.FindAll(x => x.Data.IsSelected).Select(x => x.Data.ItemResponse).ToList();
            if (selectedAnimations.Count == 0)
            {
                EditorUtility.DisplayDialog("Error", "No animations selected!", "OK");
                return;
            }

            // Disable Refresh and Import buttons
            _ui.RefreshBtn.SetEnabled(false);
            _ui.ImportBtn.SetEnabled(false);

            Items.ForEach(x => x.UpdateCanBeSelected(false));

            try
            {
                Task importTask = ServerAnimationService.ImportAnimations(selectedAnimations);

                // Show progress bar
                float progress = 0f;
                while (!importTask.IsCompleted)
                {
                    EditorUtility.DisplayProgressBar("Importing Animations", $"Progress: {progress:P0}", progress);
                    await Task.Delay(100); // Update every 100ms
                    progress += 0.01f; // Increment progress (you may want to adjust this based on actual progress)
                    if (progress > 0.99f)
                        progress = Random.value; // Set it to random value
                }

                // Ensure task is completed and handle any exceptions
                await importTask;

                EditorUtility.DisplayProgressBar("Importing Animations", "Complete!", 1f);
                await Task.Delay(500); // Show 100% for half a second
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog("Import Error", $"An error occurred during import: {e.Message}", "OK");
            }
            finally
            {
                EditorUtility.ClearProgressBar();

                // Re-enable Refresh and Import buttons
                _ui.RefreshBtn.SetEnabled(true);
                _ui.ImportBtn.SetEnabled(true);

                Items.ForEach(x => x.Reset());
            }
        }

        private void RefreshBtnOnClicked()
        {
            InjectData();
        }

        private async void InjectData()
        {
            _ui.PreviousPageBtn.SetEnabled(false);
            _ui.NextPageBtn.SetEnabled(false);
            _ui.RefreshBtn.SetEnabled(false);
            _ui.ImportBtn.SetEnabled(false);
            List<ServerAnimationItemData> list = new();
            await foreach (ServerAnimationItemResponse serverAnimationItemResponse in Data.GetAnimationItems())
                list.Add(new ServerAnimationItemData
                {
                    ItemResponse = serverAnimationItemResponse
                });
            list = list.OrderBy(item =>
            {
                return item.ItemResponse.Status.ToLower() switch
                {
                    "success" => 0,
                    "pending" or "processing" => 1,
                    "failed" => 2,
                    _ => 3
                };
            }).ToList();
            Items = _ui.ShowAnimationList(list);
            _ui.PreviousPageBtn.SetEnabled(Data.CurrentPage > 1);
            _ui.NextPageBtn.SetEnabled(Data.CurrentPage < Data.TotalPages - 1);
            _ui.RefreshBtn.SetEnabled(true);
            _ui.ImportBtn.SetEnabled(true);
        }


        private void PreviousPageBtnOnClicked()
        {
            Data.CurrentPage--;
            Data.CurrentPage = Math.Max(Data.CurrentPage, 1);
            _ui.PreviousPageBtn.SetEnabled(false);
            _ui.NextPageBtn.SetEnabled(false);
            InjectData();
        }

        private void NextPageBtnOnClicked()
        {
            if (Data.CurrentPage < Data.TotalPages - 1)
            {
                Data.CurrentPage++;
            }
            else
            {
                Data.CurrentPage = 1;
            }
            _ui.PreviousPageBtn.SetEnabled(false);
            _ui.NextPageBtn.SetEnabled(false);
            InjectData();
        }
    }

}