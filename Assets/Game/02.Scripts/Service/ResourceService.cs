using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace JumJump.Service
{
    /// <summary>
    /// Addressables 에셋 로딩/해제의 단일 접근점.
    /// 동일 키의 핸들을 캐싱해 중복 로드를 막고, Dispose 시 모든 핸들을 일괄 해제한다.
    /// </summary>
    public sealed class ResourceService : IDisposable
    {
        private readonly Dictionary<string, AsyncOperationHandle> _handles = new Dictionary<string, AsyncOperationHandle>(16);

        public async UniTask<GameObject> LoadPrefabAsync(string key, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(key))
            {
                Debug.LogError($"[{nameof(ResourceService)}] Empty resource key.");
                return null;
            }

            if (_handles.TryGetValue(key, out var cachedHandle))
            {
                return await cachedHandle.Convert<GameObject>().ToUniTask(cancellationToken: cancellationToken);
            }

            var handle = Addressables.LoadAssetAsync<GameObject>(key);
            _handles.Add(key, handle);

            var asset = await handle.ToUniTask(cancellationToken: cancellationToken);
            if (asset == null)
            {
                Debug.LogError($"[{nameof(ResourceService)}] Failed to load prefab: {key}");
            }

            return asset;
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
        }
    }
}
