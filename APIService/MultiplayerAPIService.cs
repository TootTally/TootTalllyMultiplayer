using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using static TootTallyMultiplayer.APIService.MultSerializableClasses;

namespace TootTallyMultiplayer.APIService
{
    public static class MultiplayerAPIService
    {
        public const string MULTURL = "https://spec.toottally.com/mp";


        public static IEnumerator<UnityWebRequestAsyncOperation> GetLobbyList(Action<List<MultiplayerLobbyInfo>> callback)
        {
            string query = $"{MULTURL}/list";

            UnityWebRequest webRequest = UnityWebRequest.Get(query);

            yield return webRequest.SendWebRequest();

            if (!HasError(webRequest, query))
            {
                var lobbies = JsonConvert.DeserializeObject<APIMultiplayerInfo>(webRequest.downloadHandler.text).lobbies;
                callback(lobbies);
            }
            else
                callback(null);
        }

        public static IEnumerator<UnityWebRequestAsyncOperation> ShutdownMultiplayerServer(string code, Action callback)
        {
            string query = $"{MULTURL}/delete";

            APIShutdownSubmission apiSubmission = new APIShutdownSubmission()
            {
                lobby_code = code,
            };
            var data = System.Text.Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(apiSubmission));
            UnityWebRequest webRequest = PostUploadRequestWithHeader(query, data, new List<string[]> { new string[] { "Authorization", "APIKey " + TootTallyAccounts.Plugin.GetAPIKey } });

            yield return webRequest.SendWebRequest();
            callback();
        }

        public static IEnumerator<UnityWebRequestAsyncOperation> CreateMultiplayerServerRequest(APICreateSubmission apiSubmission, Action<string> callback)
        {
            string query = $"{MULTURL}/create";
            var data = System.Text.Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(apiSubmission));
            UnityWebRequest webRequest = PostUploadRequestWithHeader(query, data, new List<string[]> { new string[] { "Authorization", "APIKey " + TootTallyAccounts.Plugin.GetAPIKey } });

            yield return webRequest.SendWebRequest();

            if (!HasError(webRequest, query))
                callback(webRequest.downloadHandler.text);
            else
                callback(null);
        }

        public static IEnumerator<UnityWebRequestAsyncOperation> TryLoadingAudioClipLocal(string fileName, Action<AudioClip> callback)
        {
            string assetDir = Path.Combine(Path.GetDirectoryName(Plugin.Instance.Info.Location), "Music");
            assetDir = Path.Combine(assetDir, fileName);
            UnityWebRequest webRequest = UnityWebRequestMultimedia.GetAudioClip("file://" + assetDir, AudioType.MPEG);
            yield return webRequest.SendWebRequest();
            if (!HasError(webRequest, assetDir))
                callback(DownloadHandlerAudioClip.GetContent(webRequest));
        }

        public static IEnumerator<UnityWebRequestAsyncOperation> TryLoadingSongAudio(string fileName, Action<AudioClip> callback)
        {
            UnityWebRequest webRequest = UnityWebRequestMultimedia.GetAudioClip("file://" + fileName, AudioType.OGGVORBIS);
            yield return webRequest.SendWebRequest();
            if (!HasError(webRequest, fileName))
                callback(DownloadHandlerAudioClip.GetContent(webRequest));
        }

        private static UnityWebRequest PostUploadRequest(string query, byte[] data, string contentType = "application/json")
        {
            DownloadHandler dlHandler = new DownloadHandlerBuffer();
            UploadHandler ulHandler = new UploadHandlerRaw(data);
            ulHandler.contentType = contentType;


            UnityWebRequest webRequest = new UnityWebRequest(query, "POST", dlHandler, ulHandler);
            return webRequest;
        }

        private static UnityWebRequest PostUploadRequestWithHeader(string query, byte[] data, List<string[]> headers, string contentType = "application/json")
        {
            DownloadHandler dlHandler = new DownloadHandlerBuffer();
            UploadHandler ulHandler = new UploadHandlerRaw(data);
            ulHandler.contentType = contentType;


            UnityWebRequest webRequest = new UnityWebRequest(query, "POST", dlHandler, ulHandler);
            foreach (string[] s in headers)
                webRequest.SetRequestHeader(s[0], s[1]);
            return webRequest;
        }

        private static bool HasError(UnityWebRequest webRequest)
        {
            return webRequest.isNetworkError || webRequest.isHttpError;
        }

        private static bool HasError(UnityWebRequest webRequest, string query)
        {
            if (webRequest.isNetworkError || webRequest.isHttpError)
                Plugin.LogError($"QUERY ERROR: {query}");
            if (webRequest.isNetworkError)
                Plugin.LogError($"NETWORK ERROR: {webRequest.error}");
            if (webRequest.isHttpError)
                Plugin.LogError($"HTTP ERROR {webRequest.error}");

            return webRequest.isNetworkError || webRequest.isHttpError;
        }
    }
}
