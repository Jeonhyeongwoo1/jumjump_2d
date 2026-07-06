using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using JumJump.Registry;
using JumJump.Util;
using UnityEngine;
using UnityEngine.Networking;
using VContainer;

namespace JumJump.Service
{
    public sealed class HttpService
    {
        private readonly AuthRegistry _authRegistry;

        [Inject]
        public HttpService(AuthRegistry authRegistry)
        {
            _authRegistry = authRegistry;
        }

        public async UniTask<TResponse> GetAsync<TResponse>(string url, CancellationToken cancellationToken = default)
        {
            using var request = UnityWebRequest.Get(url);
            return await SendAsync<TResponse>(request, cancellationToken);
        }

        public async UniTask<TResponse> GetAuthorizedAsync<TResponse>(string url, CancellationToken cancellationToken = default)
        {
            using var request = UnityWebRequest.Get(url);
            request.SetRequestHeader("Authorization", $"Bearer {_authRegistry.SessionToken}");
            return await SendAsync<TResponse>(request, cancellationToken);
        }

        public async UniTask<TResponse> PostAsync<TResponse>(string url, object body, CancellationToken cancellationToken = default)
        {
            using var request = BuildPostRequest(url, body);
            return await SendAsync<TResponse>(request, cancellationToken);
        }

        public async UniTask<TResponse> PostAuthorizedAsync<TResponse>(string url, object body, CancellationToken cancellationToken = default)
        {
            using var request = BuildPostRequest(url, body);
            request.SetRequestHeader("Authorization", $"Bearer {_authRegistry.SessionToken}");
            return await SendAsync<TResponse>(request, cancellationToken);
        }

        public TResponse ParseJson<TResponse>(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                GameLogger.Error(nameof(HttpService), "empty_response");
                return default;
            }

            return JsonUtility.FromJson<TResponse>(json);
        }

        private UnityWebRequest BuildPostRequest(string url, object body)
        {
            string json = JsonUtility.ToJson(body);
            byte[] payload = Encoding.UTF8.GetBytes(json);
            var request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST);
            request.uploadHandler = new UploadHandlerRaw(payload);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            return request;
        }

        private async UniTask<TResponse> SendAsync<TResponse>(UnityWebRequest request, CancellationToken cancellationToken)
        {
            await request.SendWebRequest().ToUniTask(cancellationToken: cancellationToken);

            if (request.result != UnityWebRequest.Result.Success)
            {
                string body = request.downloadHandler?.text ?? string.Empty;
                GameLogger.Error(nameof(HttpService), $"http_error:{request.responseCode}:{request.error}:{body}");
                return default;
            }

            return ParseJson<TResponse>(request.downloadHandler.text);
        }
    }
}
