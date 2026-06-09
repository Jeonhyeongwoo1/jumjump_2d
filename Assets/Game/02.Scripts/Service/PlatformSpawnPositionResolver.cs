using JumJump.Data;
using UnityEngine;

namespace JumJump.Service
{
    public sealed class PlatformSpawnPositionResolver
    {
        private readonly PlatformConfigData _configData;
        private readonly UnityEngine.Camera _gameCamera;

        public PlatformSpawnPositionResolver(PlatformConfigData configData, UnityEngine.Camera gameCamera)
        {
            _configData = configData;
            _gameCamera = gameCamera;
        }

        public float ResolveSpawnSide(int nextPlatformIndex)
        {
            var firstDirection = _configData.PlatformFirstSpawnDirection >= 0 ? 1f : -1f;
            if (!_configData.PlatformAlternatesSpawnSide)
            {
                return firstDirection;
            }

            if (_configData.PlatformRandomSpawnSideStartCount > 0 &&
                nextPlatformIndex + 1 >= _configData.PlatformRandomSpawnSideStartCount)
            {
                return Random.value < 0.5f ? firstDirection : -firstDirection;
            }

            return nextPlatformIndex % 2 == 0 ? firstDirection : -firstDirection;
        }

        public float ResolveIncomingSpawnX(float targetX, float spawnSide)
        {
            return targetX + spawnSide * Mathf.Max(0f, _configData.PlatformSpawnDistance);
        }

        public float ResolveDoublePreviewSpawnX(float targetX, float spawnSide)
        {
            var viewportX = spawnSide < 0f ? 0f : 1f;
            var cameraDepth = Mathf.Abs(_gameCamera.transform.position.z);
            var edgePosition = _gameCamera.ViewportToWorldPoint(new Vector3(viewportX, 0.5f, cameraDepth));
            return edgePosition.x;
        }
    }
}
