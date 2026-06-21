using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using JumJump.Data;
using JumJump.Util;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using Object = UnityEngine.Object;

namespace JumJump.Service
{
    public sealed class ResourceService : IDisposable
    {
        private const string SpriteKeySuffix = ".sprite";

        private readonly Dictionary<string, Object> _resources = new Dictionary<string, Object>(32);
        private readonly Dictionary<string, AsyncOperationHandle> _handles = new Dictionary<string, AsyncOperationHandle>(32);
        private readonly Dictionary<string, HashSet<string>> _labelResourceKeys = new Dictionary<string, HashSet<string>>(4);
        private readonly Dictionary<string, HashSet<string>> _resourceLabels = new Dictionary<string, HashSet<string>>(32);
        private readonly HashSet<string> _loadedLabels = new HashSet<string>();
        private readonly ResourceConfigData _configData;

        private bool _isPreLoaded;

        public ResourceService(ResourceConfigData configData)
        {
            _configData = configData;
        }

        public GameObject GetPrefab(string key)
        {
            return GetAsset<GameObject>(key);
        }

        public T GetAsset<T>(string key) where T : Object
        {
            var resolvedKey = ResolveKey<T>(key);
            if (string.IsNullOrWhiteSpace(resolvedKey))
            {
                GameLogger.Error(nameof(ResourceService), "Empty resource key.");
                return null;
            }

            if (!_isPreLoaded)
            {
                GameLogger.Error(nameof(ResourceService), $"Asset requested before preload: {resolvedKey}");
                return null;
            }

            if (!_resources.TryGetValue(resolvedKey, out var resource))
            {
                GameLogger.Error(nameof(ResourceService), $"Preloaded asset not found: {resolvedKey}");
                return null;
            }

            if (resource is T typedResource)
            {
                return typedResource;
            }

            GameLogger.Error(
                nameof(ResourceService),
                $"Preloaded asset type mismatch. Key: {resolvedKey}, Cached: {resource.GetType().Name}, Requested: {typeof(T).Name}");
            return null;
        }

        public async UniTask PreLoadAsync(CancellationToken cancellationToken = default)
        {
            if (_isPreLoaded)
            {
                return;
            }

            var preLoadLabel = _configData.PreLoadLabel;
            var loaded = await LoadLabelAsync(preLoadLabel, cancellationToken);
            _isPreLoaded = loaded;
        }

        public async UniTask<bool> LoadLabelAsync(string labelKey, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(labelKey))
            {
                GameLogger.Error(nameof(ResourceService), "Addressables label is empty.");
                return false;
            }

            if (_loadedLabels.Contains(labelKey))
            {
                return true;
            }

            var loaded = await LoadResourcesAsync(labelKey, labelKey, cancellationToken);
            if (!loaded)
            {
                return false;
            }

            _loadedLabels.Add(labelKey);
            return true;
        }

        public async UniTask<bool> LoadKeyAsync(string key, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                GameLogger.Error(nameof(ResourceService), "Addressables key is empty.");
                return false;
            }

            if (IsResourceCached(key))
            {
                return true;
            }

            return await LoadResourcesAsync(key, null, cancellationToken);
        }

        public void Release(string key)
        {
            ReleaseCachedResource(key);
        }

        public async UniTask ReleaseLabelAsync(string labelKey, bool unloadUnusedAssets = false)
        {
            if (string.IsNullOrWhiteSpace(labelKey))
            {
                return;
            }

            if (_labelResourceKeys.TryGetValue(labelKey, out var resourceKeys))
            {
                foreach (var resourceKey in resourceKeys)
                {
                    if (HasOtherLabelOwner(resourceKey, labelKey))
                    {
                        continue;
                    }

                    ReleaseCachedResource(resourceKey);
                }

                _labelResourceKeys.Remove(labelKey);
            }

            _loadedLabels.Remove(labelKey);

            if (unloadUnusedAssets)
            {
                await Resources.UnloadUnusedAssets().ToUniTask();
                GC.Collect();
            }
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

            _resources.Clear();
            _handles.Clear();
            _labelResourceKeys.Clear();
            _resourceLabels.Clear();
            _loadedLabels.Clear();
            _isPreLoaded = false;
        }

        private async UniTask<bool> LoadResourcesAsync(
            string key,
            string labelKey,
            CancellationToken cancellationToken)
        {
            var locationsHandle = Addressables.LoadResourceLocationsAsync(key);
            try
            {
                var locations = await locationsHandle.ToUniTask(cancellationToken: cancellationToken);
                if (locations == null || locations.Count == 0)
                {
                    GameLogger.Error(nameof(ResourceService), $"Addressables key returned no locations: {key}");
                    return false;
                }

                GameLogger.Debug(nameof(ResourceService), $"Resolved {locations.Count} addressable assets from: {key}");

                var failedCount = 0;
                for (var i = 0; i < locations.Count; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var location = locations[i];
                    if (location == null || string.IsNullOrEmpty(location.PrimaryKey))
                    {
                        failedCount++;
                        continue;
                    }

                    if (IsResourceCached(location.PrimaryKey))
                    {
                        TrackLabelResource(labelKey, location.PrimaryKey);
                        continue;
                    }

                    var loaded = await LoadAssetAsync(location, cancellationToken);
                    if (!loaded)
                    {
                        failedCount++;
                    }
                    else
                    {
                        GameLogger.Debug(nameof(ResourceService), $"Loaded asset: {location.PrimaryKey}");
                    }

                    TrackLabelResource(labelKey, location.PrimaryKey);
                    await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
                }

                if (failedCount > 0)
                {
                    GameLogger.Error(nameof(ResourceService), $"Failed to load {failedCount} addressable assets from: {key}");
                    return false;
                }

                return true;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                GameLogger.Error(nameof(ResourceService), $"Failed to load addressables: {key}\n{ex}");
                return false;
            }
            finally
            {
                if (locationsHandle.IsValid())
                {
                    Addressables.Release(locationsHandle);
                }
            }
        }

        private UniTask<bool> LoadAssetAsync(IResourceLocation location, CancellationToken cancellationToken)
        {
            var key = location.PrimaryKey;
            if (key.EndsWith(SpriteKeySuffix, StringComparison.Ordinal))
            {
                return LoadTypedAssetByKeyAsync<Sprite>(key, cancellationToken);
            }

            if (location.ResourceType == typeof(Sprite))
            {
                return LoadTypedAssetAsync<Sprite>(location, key, cancellationToken);
            }

            return LoadTypedAssetAsync<Object>(location, key, cancellationToken);
        }

        private async UniTask<bool> LoadTypedAssetByKeyAsync<T>(string key, CancellationToken cancellationToken)
            where T : Object
        {
            try
            {
                var handle = Addressables.LoadAssetAsync<T>(key);
                var resource = await handle.ToUniTask(cancellationToken: cancellationToken);
                if (resource == null)
                {
                    GameLogger.Error(nameof(ResourceService), $"Failed to load addressable asset. Key: {key}, Type: {typeof(T).Name}");
                    ReleaseHandle(handle);
                    return false;
                }

                CacheResource(key, resource, handle);
                return true;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                GameLogger.Error(nameof(ResourceService), $"Failed to load addressable asset. Key: {key}, Type: {typeof(T).Name}\n{ex}");
                return false;
            }
        }

        private async UniTask<bool> LoadTypedAssetAsync<T>(
            IResourceLocation location,
            string key,
            CancellationToken cancellationToken)
            where T : Object
        {
            try
            {
                var handle = Addressables.LoadAssetAsync<T>(location);
                var resource = await handle.ToUniTask(cancellationToken: cancellationToken);
                if (resource == null)
                {
                    GameLogger.Error(nameof(ResourceService), $"Failed to load addressable asset. Key: {key}, Type: {typeof(T).Name}");
                    ReleaseHandle(handle);
                    return false;
                }

                CacheResource(key, resource, handle);
                return true;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                GameLogger.Error(nameof(ResourceService), $"Failed to load addressable asset. Key: {key}, Type: {typeof(T).Name}\n{ex}");
                return false;
            }
        }

        private void CacheResource(string key, Object resource, AsyncOperationHandle handle)
        {
            if (string.IsNullOrEmpty(key))
            {
                ReleaseHandle(handle);
                return;
            }

            if (_resources.ContainsKey(key))
            {
                ReleaseHandle(handle);
                return;
            }

            _resources.Add(key, resource);
            _handles.Add(key, handle);

            if (resource is Sprite)
            {
                CacheSpriteAlias(key, resource);
            }
        }

        private void CacheSpriteAlias(string key, Object resource)
        {
            if (key.EndsWith(SpriteKeySuffix, StringComparison.Ordinal))
            {
                var baseKey = key.Substring(0, key.Length - SpriteKeySuffix.Length);
                if (!_resources.ContainsKey(baseKey))
                {
                    _resources.Add(baseKey, resource);
                }

                return;
            }

            var spriteKey = key + SpriteKeySuffix;
            if (!_resources.ContainsKey(spriteKey))
            {
                _resources.Add(spriteKey, resource);
            }
        }

        private bool IsResourceCached(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return false;
            }

            if (_resources.ContainsKey(key))
            {
                return true;
            }

            if (!key.EndsWith(SpriteKeySuffix, StringComparison.Ordinal))
            {
                return false;
            }

            var baseKey = key.Substring(0, key.Length - SpriteKeySuffix.Length);
            return _resources.TryGetValue(baseKey, out var resource) && resource is Sprite;
        }

        private string ResolveKey<T>(string key) where T : Object
        {
            if (typeof(T) == typeof(Sprite) &&
                !string.IsNullOrEmpty(key) &&
                !key.EndsWith(SpriteKeySuffix, StringComparison.Ordinal))
            {
                return key + SpriteKeySuffix;
            }

            return key;
        }

        private void TrackLabelResource(string labelKey, string resourceKey)
        {
            if (string.IsNullOrEmpty(labelKey) || string.IsNullOrEmpty(resourceKey))
            {
                return;
            }

            if (!_labelResourceKeys.TryGetValue(labelKey, out var resourceKeys))
            {
                resourceKeys = new HashSet<string>();
                _labelResourceKeys.Add(labelKey, resourceKeys);
            }

            resourceKeys.Add(resourceKey);

            if (!_resourceLabels.TryGetValue(resourceKey, out var labels))
            {
                labels = new HashSet<string>();
                _resourceLabels.Add(resourceKey, labels);
            }

            labels.Add(labelKey);
        }

        private bool HasOtherLabelOwner(string resourceKey, string labelKey)
        {
            if (!_resourceLabels.TryGetValue(resourceKey, out var labels))
            {
                return false;
            }

            labels.Remove(labelKey);
            if (labels.Count > 0)
            {
                return true;
            }

            _resourceLabels.Remove(resourceKey);
            return false;
        }

        private void ReleaseCachedResource(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return;
            }

            var resolvedKey = ResolveReleaseKey(key);
            _resources.TryGetValue(resolvedKey, out var resource);
            _resources.Remove(resolvedKey);
            ReleaseHandle(resolvedKey);

            if (resolvedKey.EndsWith(SpriteKeySuffix, StringComparison.Ordinal))
            {
                var baseKey = resolvedKey.Substring(0, resolvedKey.Length - SpriteKeySuffix.Length);
                ReleaseAlias(baseKey, resource);
                return;
            }

            ReleaseAlias(resolvedKey + SpriteKeySuffix, resource);
        }

        private string ResolveReleaseKey(string key)
        {
            if (_resources.ContainsKey(key))
            {
                return key;
            }

            var spriteKey = key + SpriteKeySuffix;
            if (_resources.ContainsKey(spriteKey))
            {
                return spriteKey;
            }

            return key;
        }

        private void ReleaseAlias(string key, Object resource)
        {
            if (resource == null || !_resources.TryGetValue(key, out var aliasResource))
            {
                return;
            }

            if (!ReferenceEquals(resource, aliasResource))
            {
                return;
            }

            _resources.Remove(key);
            ReleaseHandle(key);
        }

        private void ReleaseHandle(string key)
        {
            if (!_handles.TryGetValue(key, out var handle))
            {
                return;
            }

            ReleaseHandle(handle);
            _handles.Remove(key);
        }

        private void ReleaseHandle(AsyncOperationHandle handle)
        {
            if (handle.IsValid())
            {
                Addressables.Release(handle);
            }
        }
    }
}
