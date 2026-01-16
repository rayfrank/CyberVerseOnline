using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Convai.Scripts.Editor.Setup.ServerAnimation
{

    public static class ServerAnimationAPI
    {
        private const string BASE_URL = "https://api.convai.com/";
        private const string GET_ANIMATION_LIST = "animations/list";
        private const string GET_ANIMATION = "animations/get";

        public static async Task<ServerAnimationListResponse> GetAnimationList(string apiKey, int page)
        {
            HttpClient client = CreateHttpClient(apiKey);
            HttpContent content = CreateHttpContent(new Dictionary<string, object> {
                { "status", "success" },
                { "generate_signed_urls", true },
                { "page", page }
            });
            string response = await SendPostRequestAsync(GetEndPoint(GET_ANIMATION_LIST), client, content);
            ServerAnimationListResponse serverAnimationListResponse = JsonConvert.DeserializeObject<ServerAnimationListResponse>(response);
            return serverAnimationListResponse;
        }

        public static async Task<bool> DownloadAnimation(string animationID, string apiKey, string saveDirectory, string newFileName)
        {
            HttpClient client = CreateHttpClient(apiKey);
            HttpContent content = CreateHttpContent(new Dictionary<string, object> {
                { "animation_id", animationID },
                { "generate_upload_video_urls", false }
            });
            string response = await SendPostRequestAsync(GetEndPoint(GET_ANIMATION), client, content);
            dynamic animation = JsonConvert.DeserializeObject(response);
            if (animation == null)
                return false;
            string gcpLink = animation.animation.fbx_gcp_file;

            using HttpClient downloadClient = new();
            try
            {
                byte[] fileBytes = await downloadClient.GetByteArrayAsync(gcpLink);

                // Ensure the save directory exists
                if (!Directory.Exists(saveDirectory))
                    Directory.CreateDirectory(saveDirectory);

                // Construct the full file path with the new name and .fbx extension
                string filePath = Path.Combine(saveDirectory, $"{newFileName}.fbx");
                int counter = 1;

                while (File.Exists(filePath))
                {
                    filePath = Path.Combine(saveDirectory, $"{newFileName}_{counter}.fbx");
                    counter++;
                }

                // Write the file
                await File.WriteAllBytesAsync(filePath, fileBytes);
                string relativePath = filePath.Substring(Application.dataPath.Length + 1).Replace('\\', '/');
                relativePath = "Assets/" + relativePath;
                //AssetDatabase.ImportAsset(relativePath);
                AssetDatabase.Refresh();
                ModelImporter importer = AssetImporter.GetAtPath(relativePath) as ModelImporter;
                if (importer != null)
                {
                    importer.animationType = ModelImporterAnimationType.Human;
                    importer.importAnimatedCustomProperties = true;
                    importer.materialLocation = ModelImporterMaterialLocation.External;
                    importer.SaveAndReimport();
                }
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error downloading file: {ex.Message}");
                return false;
            }
        }

        public static async Task<Texture2D> GetThumbnail(string thumbnailURL)
        {
            using HttpClient client = new();
            try
            {
                byte[] fileBytes = await client.GetByteArrayAsync(thumbnailURL);
                Texture2D texture = new(256, 256);
                texture.LoadImage(fileBytes);
                return texture;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error downloading thumbnail: {ex.Message}");
                return null;
            }
        }


        private static string GetEndPoint(string endpoint)
        {
            return BASE_URL + endpoint;
        }


        private static HttpClient CreateHttpClient(string apiKey)
        {
            if (string.IsNullOrEmpty(apiKey))
                return new HttpClient();
            HttpClient httpClient = new()
            {
                Timeout = TimeSpan.FromSeconds(30),
                DefaultRequestHeaders = {
                    Accept = {
                        new MediaTypeWithQualityHeaderValue( "application/json" )
                    }
                }
            };
            httpClient.DefaultRequestHeaders.Add("CONVAI-API-KEY", apiKey);
            httpClient.DefaultRequestHeaders.Add("Source", "convaiUI");
            return httpClient;
        }

        private static HttpContent CreateHttpContent(Dictionary<string, object> dataToSend)
        {
            // Serialize the dictionary to JSON
            string json = JsonConvert.SerializeObject(dataToSend);

            // Convert JSON to HttpContent
            return new StringContent(json, Encoding.UTF8, "application/json");
        }

        private static HttpContent CreateHttpContent(string json)
        {
            // Convert JSON to HttpContent
            return new StringContent(json, Encoding.UTF8, "application/json");
        }

        private static async Task<string> SendPostRequestAsync(string endpoint, HttpClient httpClient, HttpContent content)
        {
            try
            {
                HttpResponseMessage response = await httpClient.PostAsync(endpoint, content);
                response.EnsureSuccessStatusCode();
                string responseContent = await response.Content.ReadAsStringAsync();
                return responseContent;
            }
            catch (HttpRequestException e)
            {
                Debug.Log($"Request to {endpoint} failed: {e.Message}");
                return null;
            }
        }
    }


    [Serializable]
    public class ServerAnimationListResponse
    {
        [JsonProperty("animations")] public List<ServerAnimationItemResponse> Animations { get; private set; }
        [JsonProperty("transaction_id")] public string TransactionID { get; private set; }
        [JsonProperty("total_pages")] public int TotalPages { get; private set; }
        [JsonProperty("page")] public int CurrentPage { get; private set; }
        [JsonProperty("total")] public int TotalItems { get; private set; }
    }

    [Serializable]
    public class ServerAnimationItemResponse
    {
        public ServerAnimationItemResponse(string animationID, string animationName, string status, string thumbnailURL)
        {
            AnimationID = animationID;
            AnimationName = animationName;
            Status = status;
            ThumbnailURL = thumbnailURL;
        }

        [JsonProperty("animation_id")] public string AnimationID { get; private set; }
        [JsonProperty("animation_name")] public string AnimationName { get; private set; }
        [JsonProperty("status")] public string Status { get; private set; }
        [JsonProperty("thumbnail_gcp_file")] public string ThumbnailURL { get; private set; }
    }

}