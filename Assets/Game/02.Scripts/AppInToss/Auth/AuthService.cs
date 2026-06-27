using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using JumJump.Bridge;
using JumJump.Data;
using JumJump.Event;
using JumJump.Interface;
using JumJump.Registry;
using UnityEngine;
using VContainer.Unity;

namespace JumJump.Service
{
    public sealed class AuthService : IInitializable, IAsyncStartable, IDisposable
    {
        private const string SessionTokenKey = "appintoss_session_token";
        private const string UserIdKey = "appintoss_user_id";
        private const string HasRemovedAdsKey = "appintoss_has_removed_ads";

        private readonly IEventBus _eventBus;
        private readonly AppInTossConfigSO _config;
        private readonly AuthRegistry _authRegistry;
        private readonly HttpService _httpClient;
        private readonly PlayerDataRegistry _playerDataRegistry;

        private CancellationTokenSource _cancellationTokenSource;

        private string LoginUrl => _config.CloudFunctionBaseUrl + AppInTossConfigSO.LoginPath;
        private string PlayerMeUrl => _config.CloudFunctionBaseUrl + AppInTossConfigSO.PlayerMePath;
        private string RecordAdRemovalPurchaseUrl =>
            _config.CloudFunctionBaseUrl + AppInTossConfigSO.RecordAdRemovalPurchasePath;
        private string SavePlayerProgressUrl =>
            _config.CloudFunctionBaseUrl + AppInTossConfigSO.SavePlayerProgressPath;

        public AuthService(
            IEventBus eventBus,
            AppInTossConfigSO config,
            AuthRegistry authRegistry,
            HttpService httpClient,
            PlayerDataRegistry playerDataRegistry)
        {
            _eventBus = eventBus;
            _config = config;
            _authRegistry = authRegistry;
            _httpClient = httpClient;
            _playerDataRegistry = playerDataRegistry;
        }

        public void Initialize()
        {
            _eventBus.Subscribe<PlayerProgressSavedEvent>(OnPlayerProgressSaved);
        }

        public async UniTask StartAsync(CancellationToken cancellation)
        {
            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellation);

            try
            {
                ApiSchema.LoginResponse response = await LoginAsync(_cancellationTokenSource.Token);
                if (!ApplyLoginResponse(response))
                {
                    _eventBus.Publish(new AuthLoginFailedEvent("invalid_login_response"));
                    return;
                }

                await SavePlayerProgressInternalAsync(
                    _playerDataRegistry.HighScore,
                    _playerDataRegistry.Gold,
                    _playerDataRegistry.SelectedPlayerSkinId,
                    false,
                    _cancellationTokenSource.Token);

                _eventBus.Publish(new AuthLoginCompletedEvent(
                    _authRegistry.UserId,
                    _authRegistry.Nickname,
                    _authRegistry.HasRemovedAds));
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{nameof(AuthService)}] Login failed: {ex.Message}");
                _eventBus.Publish(new AuthLoginFailedEvent(ex.Message));
            }
        }

        public void Dispose()
        {
            _eventBus.Unsubscribe<PlayerProgressSavedEvent>(OnPlayerProgressSaved);
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }

        public async UniTask<bool> RefreshUserAsync(CancellationToken cancellationToken = default)
        {
            if (!EnsureLoggedIn())
            {
                return false;
            }

            var response = await _httpClient.GetAuthorizedAsync<ApiSchema.UserResponse>(PlayerMeUrl, cancellationToken);
            if (!ApplyUserData(response?.User))
            {
                return false;
            }

            _eventBus.Publish(new AuthUserRefreshedEvent(_authRegistry.UserId, _authRegistry.Nickname, _authRegistry.HasRemovedAds));
            return true;
        }

        public UniTask<bool> SavePlayerProgressAsync(
            int highScore,
            int gold,
            int selectedPlayerSkinId,
            CancellationToken cancellationToken = default)
        {
            return SavePlayerProgressInternalAsync(
                highScore,
                gold,
                selectedPlayerSkinId,
                true,
                cancellationToken);
        }

        public async UniTask<bool> RecordAdRemovalPurchaseAsync(
            string orderId,
            string productId,
            long purchasedAtMillis,
            CancellationToken cancellationToken = default)
        {
            if (!EnsureLoggedIn())
            {
                return false;
            }

            var body = new ApiSchema.AdRemovalPurchaseRequest(orderId, productId, purchasedAtMillis);
            var response = await _httpClient.PostAuthorizedAsync<ApiSchema.UserResponse>(
                RecordAdRemovalPurchaseUrl,
                body,
                cancellationToken);
            if (!ApplyUserData(response?.User))
            {
                return false;
            }

            _eventBus.Publish(new AuthUserRefreshedEvent(_authRegistry.UserId, _authRegistry.Nickname, _authRegistry.HasRemovedAds));
            return true;
        }

        private async UniTask<bool> SavePlayerProgressInternalAsync(
            int highScore,
            int gold,
            int selectedPlayerSkinId,
            bool publishRefreshEvent,
            CancellationToken cancellationToken)
        {
            if (!EnsureLoggedIn())
            {
                return false;
            }

            var body = new ApiSchema.PlayerProgressRequest(highScore, gold, selectedPlayerSkinId);
            var response = await _httpClient.PostAuthorizedAsync<ApiSchema.UserResponse>(
                SavePlayerProgressUrl,
                body,
                cancellationToken);
            if (!ApplyUserData(response?.User))
            {
                return false;
            }

            if (publishRefreshEvent)
            {
                _eventBus.Publish(new AuthUserRefreshedEvent(_authRegistry.UserId, _authRegistry.Nickname, _authRegistry.HasRemovedAds));
            }

            return true;
        }

        private void OnPlayerProgressSaved(in PlayerProgressSavedEvent ev)
        {
            if (!_authRegistry.IsLoggedIn)
            {
                return;
            }

            SaveProgressAfterLocalSaveAsync(ev, ResolveSaveCancellationToken()).Forget();
        }

        private async UniTask SaveProgressAfterLocalSaveAsync(
            PlayerProgressSavedEvent ev,
            CancellationToken cancellationToken)
        {
            try
            {
                await SavePlayerProgressInternalAsync(
                    ev.HighScore,
                    ev.Gold,
                    ev.SelectedPlayerSkinId,
                    true,
                    cancellationToken);
            }
            catch (OperationCanceledException)
            {
                Debug.LogWarning($"[{nameof(AuthService)}] progress_save_cancelled");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{nameof(AuthService)}] progress_save_failed: {ex.Message}");
            }
        }

        private CancellationToken ResolveSaveCancellationToken()
        {
            return _cancellationTokenSource?.Token ?? CancellationToken.None;
        }

        private async UniTask<ApiSchema.LoginResponse> LoginAsync(CancellationToken cancellationToken)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            AppInTossAuthWebGL.StartLogin(LoginUrl);
            await UniTask.WaitUntil(AppInTossAuthWebGL.IsLoginCompleted, cancellationToken: cancellationToken)
                .Timeout(TimeSpan.FromMilliseconds(_config.WebGLLoginTimeoutMs));

            string error = AppInTossAuthWebGL.GetLoginError();
            if (!string.IsNullOrEmpty(error))
            {
                Debug.LogError($"[{nameof(AuthService)}] WebGL login error: {error}");
                return null;
            }

            return _httpClient.ParseJson<ApiSchema.LoginResponse>(AppInTossAuthWebGL.GetLoginResultJson());
#else
            var body = new ApiSchema.LoginRequest(_config.EditorTossHash);
            return await _httpClient.PostAsync<ApiSchema.LoginResponse>(LoginUrl, body, cancellationToken);
#endif
        }

        private bool ApplyLoginResponse(ApiSchema.LoginResponse response)
        {
            if (response == null ||
                string.IsNullOrEmpty(response.SessionToken) ||
                response.User == null ||
                string.IsNullOrEmpty(response.User.UserId))
            {
                Debug.LogError($"[{nameof(AuthService)}] invalid_login_response");
                return false;
            }

            _authRegistry.SetSessionToken(response.SessionToken);
            PlayerPrefs.SetString(SessionTokenKey, response.SessionToken);
            ApplyUserData(response.User);
            PlayerPrefs.Save();
            return true;
        }

        private bool ApplyUserData(ApiSchema.UserData userData)
        {
            if (userData == null || string.IsNullOrEmpty(userData.UserId))
            {
                Debug.LogError($"[{nameof(AuthService)}] invalid_user_response");
                return false;
            }

            _authRegistry.SetUserData(
                userData.UserId,
                userData.HasRemovedAds,
                userData.Nickname,
                userData.CreatedAtMillis);
            _playerDataRegistry.ApplyServerProgress(
                userData.HighScore,
                userData.Gold,
                userData.SelectedPlayerSkinId);
            PlayerPrefs.SetString(UserIdKey, userData.UserId);
            PlayerPrefs.SetInt(HasRemovedAdsKey, userData.HasRemovedAds ? 1 : 0);
            PlayerPrefs.Save();
            return true;
        }

        private bool EnsureLoggedIn()
        {
            if (_authRegistry.IsLoggedIn)
            {
                return true;
            }

            Debug.LogError($"[{nameof(AuthService)}] auth_required");
            return false;
        }
    }
}
