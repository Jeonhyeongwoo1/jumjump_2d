using System;
using System.Collections.Generic;
using JumJump.Util;
using UnityEngine;
using UnityEngine.Pool;
using VContainer;

namespace JumJump.Service
{
    public sealed class PoolService
    {
        private readonly Dictionary<string, IPool> _pools = new Dictionary<string, IPool>(16);

        [Inject]
        public PoolService()
        {
        }

        public void Register<T>(
            string key,
            Func<T> createFunc,
            Action<T> onGet,
            Action<T> onRelease,
            int prewarmCount,
            int maxSize = int.MaxValue) where T : Component
        {
            if (_pools.ContainsKey(key))
            {
                GameLogger.Warning(nameof(PoolService), $"Pool already registered: {key}");
                return;
            }

            var pool = new ComponentPool<T>(createFunc, onGet, onRelease, prewarmCount, maxSize);
            pool.Prewarm(prewarmCount);
            _pools.Add(key, pool);
        }

        public T Get<T>(string key) where T : Component
        {
            if (!_pools.TryGetValue(key, out var pool))
            {
                GameLogger.Error(nameof(PoolService), $"Pool not registered: {key}");
                return null;
            }

            if (pool is not ComponentPool<T> typedPool)
            {
                GameLogger.Error(nameof(PoolService), $"Pool type mismatch: {key}");
                return null;
            }

            return typedPool.Get();
        }

        public void Release<T>(string key, T instance) where T : Component
        {
            if (instance == null)
            {
                return;
            }

            if (!_pools.TryGetValue(key, out var pool))
            {
                GameLogger.Error(nameof(PoolService), $"Pool not registered: {key}");
                return;
            }

            if (pool is not ComponentPool<T> typedPool)
            {
                GameLogger.Error(nameof(PoolService), $"Pool type mismatch: {key}");
                return;
            }

            typedPool.Release(instance);
        }

        private interface IPool
        {
        }

        private sealed class ComponentPool<T> : IPool where T : Component
        {
            private readonly ObjectPool<T> _objectPool;

            public ComponentPool(Func<T> createFunc, Action<T> onGet, Action<T> onRelease, int defaultCapacity, int maxSize)
            {
                _objectPool = new ObjectPool<T>(
                    createFunc,
                    onGet,
                    onRelease,
                    null,
                    true,
                    Mathf.Max(1, defaultCapacity),
                    maxSize);
            }

            public void Prewarm(int count)
            {
                var instances = new List<T>(count);

                for (var i = 0; i < count; i++)
                {
                    var instance = _objectPool.Get();
                    if (instance != null)
                    {
                        instances.Add(instance);
                    }
                }

                for (var i = 0; i < instances.Count; i++)
                {
                    _objectPool.Release(instances[i]);
                }
            }

            public T Get()
            {
                return _objectPool.Get();
            }

            public void Release(T instance)
            {
                _objectPool.Release(instance);
            }
        }
    }
}
