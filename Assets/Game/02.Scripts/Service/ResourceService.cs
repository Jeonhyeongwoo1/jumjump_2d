using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using JumJump.Data;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;

namespace JumJump.Service
{
    /// <summary>
    /// Addressables 에셋 로딩/해제의 단일 접근점.
    /// 동일 키의 핸들을 캐싱해 중복 로드를 막고, Dispose 시 모든 핸들을 일괄 해제한다.
    /// </summary>
    public sealed class ResourceService : IDisposable
    {
        private readonly Dictionary<string, UnityEngine.Object> _assets = new Dictionary<string, UnityEngine.Object>(16);
        private readonly Dictionary<string, AsyncOperationHandle> _handles = new Dictionary<string, AsyncOperationHandle>(16);
        private readonly ResourceConfigData _configData;
        private readonly PlatformConfigData _platformConfigData;
        private bool _isPreLoaded;

        public ResourceService(ResourceConfigData configData, PlatformConfigData platformConfigData)
        {
            _configData = configData;
            _platformConfigData = platformConfigData;
        }

        public GameObject GetPrefab(string key)
        {
            return GetAsset<GameObject>(key);
        }

        public T GetAsset<T>(string key) where T : UnityEngine.Object
        {
            if (string.IsNullOrEmpty(key))
            {
                Debug.LogError($"[{nameof(ResourceService)}] Empty resource key.");
                return null;
            }

            if (!_isPreLoaded)
            {
                Debug.LogError($"[{nameof(ResourceService)}] Asset requested before preload: {key}");
                return null;
            }

            if (!_assets.TryGetValue(key, out var asset))
            {
                Debug.LogError($"[{nameof(ResourceService)}] Preloaded asset not found: {key}");
                return null;
            }

            if (asset is T typedAsset)
            {
                return typedAsset;
            }

            Debug.LogError($"[{nameof(ResourceService)}] Preloaded asset type mismatch: {key} ({asset.GetType().Name})");
            return null;
        }

        public async UniTask PreLoadAsync(CancellationToken cancellationToken = default)
        {
            if (_isPreLoaded)
            {
                return;
            }

            var preLoadLabel = _configData.PreLoadLabel;
            var locationsHandle = Addressables.LoadResourceLocationsAsync(preLoadLabel);
            try
            {
                var locations = await locationsHandle.ToUniTask(cancellationToken: cancellationToken);

                if (locations == null || locations.Count == 0)
                {
                    Debug.LogWarning($"[{nameof(ResourceService)}] No addressables found for label: {preLoadLabel}");
                }
                else
                {
                    await LoadPreLoadLocationsAsync(locations, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{nameof(ResourceService)}] Failed to load addressables by label: {preLoadLabel}\n{ex}");
            }
            finally
            {
                ReleaseLocationHandle(locationsHandle);
            }

            await EnsureConfiguredAssetsLoadedAsync(cancellationToken);
            _isPreLoaded = true;
        }

        public void Release(string key)
        {
            if (!_handles.TryGetValue(key, out var handle))
            {
                return;
            }

            if (handle.IsValid())
            {
                Addressables.Release(handle);
            }

            _handles.Remove(key);
            _assets.Remove(key);
        }

        public void Dispose()
        {
            foreach (var handle in _handles.Values)
            {
                if (handle.IsValid())
                {
                    Addressables.Release(handle);
                }
            }

            _handles.Clear();
            _assets.Clear();
            _isPreLoaded = false;
        }

        private async UniTask LoadPreLoadLocationsAsync(IList<IResourceLocation> locations, CancellationToken cancellationToken)
        {
            for (var i = 0; i < locations.Count; i++)
            {
                var location = locations[i];
                if (location == null || string.IsNullOrEmpty(location.PrimaryKey))
                {
                    continue;
                }

                if (_handles.ContainsKey(location.PrimaryKey))
                {
                    continue;
                }

                if (typeof(GameObject).IsAssignableFrom(location.ResourceType))
                {
                    await TryLoadPreLoadAssetAsync<GameObject>(location, cancellationToken);
                    continue;
                }

                if (typeof(UnityEngine.Object).IsAssignableFrom(location.ResourceType))
                {
                    await TryLoadPreLoadAssetAsync<UnityEngine.Object>(location, cancellationToken);
                    continue;
                }

                Debug.LogWarning($"[{nameof(ResourceService)}] Unsupported preload asset type: {location.PrimaryKey} ({location.ResourceType})");
            }
        }

        private async UniTask TryLoadPreLoadAssetAsync<T>(IResourceLocation location, CancellationToken cancellationToken)
            where T : UnityEngine.Object
        {
            AsyncOperationHandle<T> handle;
            try
            {
                handle = Addressables.LoadAssetAsync<T>(location);
                SetHandle(location.PrimaryKey, handle);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{nameof(ResourceService)}] Failed to start preload asset: {location.PrimaryKey}\n{ex}");
                return;
            }

            try
            {
                var asset = await handle.ToUniTask(cancellationToken: cancellationToken);
                if (asset == null)
                {
                    Debug.LogError($"[{nameof(ResourceService)}] Failed to preload asset: {location.PrimaryKey}");
                    return;
                }

                CacheAsset(location.PrimaryKey, asset);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{nameof(ResourceService)}] Failed to preload asset: {location.PrimaryKey}\n{ex}");
            }
        }

        private async UniTask EnsureConfiguredAssetsLoadedAsync(CancellationToken cancellationToken)
        {
            await EnsureAssetLoadedAsync<GameObject>(_configData.PlatformAddressableKey, cancellationToken);
            await EnsureAssetLoadedAsync<GameObject>(_configData.PlayerAddressableKey, cancellationToken);
            await EnsureAssetLoadedAsync<GameObject>(_configData.GameSceneUiAddressableKey, cancellationToken);
            await EnsureAssetLoadedAsync<GameObject>(_configData.GameOverPopupAddressableKey, cancellationToken);
            await EnsureAssetLoadedAsync<GameObject>(_configData.DynamicFontAddressableKey, cancellationToken);
            await EnsureAssetLoadedAsync<Sprite>(_platformConfigData.PlatformNormalSpriteAddressableKey, cancellationToken);
            await EnsureAssetLoadedAsync<Sprite>(_platformConfigData.PlatformShieldSpriteAddressableKey, cancellationToken);
            await EnsureAssetLoadedAsync<Sprite>(_platformConfigData.PlatformRocketSpriteAddressableKey, cancellationToken);
        }

        private async UniTask EnsureAssetLoadedAsync<T>(string key, CancellationToken cancellationToken)
            where T : UnityEngine.Object
        {
            if (string.IsNullOrEmpty(key) || HasCachedAsset<T>(key))
            {
                return;
            }

            AsyncOperationHandle<T> handle;
            try
            {
                handle = Addressables.LoadAssetAsync<T>(key);
                SetHandle(key, handle);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{nameof(ResourceService)}] Failed to start configured asset preload: {key}\n{ex}");
                return;
            }

            try
            {
                var asset = await handle.ToUniTask(cancellationToken: cancellationToken);
                if (asset == null)
                {
                    Debug.LogError($"[{nameof(ResourceService)}] Failed to preload configured asset: {key}");
                    return;
                }

                CacheAsset(key, asset);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{nameof(ResourceService)}] Failed to preload configured asset: {key}\n{ex}");
            }
        }

        private void CacheAsset(string key, UnityEngine.Object asset)
        {
            if (string.IsNullOrEmpty(key))
            {
                return;
            }

            _assets[key] = asset;
        }

        private bool HasCachedAsset<T>(string key) where T : UnityEngine.Object
        {
            return _assets.TryGetValue(key, out var asset) && asset is T;
        }

        private void SetHandle(string key, AsyncOperationHandle handle)
        {
            if (_handles.TryGetValue(key, out var previousHandle) && previousHandle.IsValid())
            {
                Addressables.Release(previousHandle);
            }

            _handles[key] = handle;
        }

        private void ReleaseLocationHandle(AsyncOperationHandle<IList<IResourceLocation>> handle)
        {
            if (handle.IsValid())
            {
                Addressables.Release(handle);
            }
        }
    }
}
