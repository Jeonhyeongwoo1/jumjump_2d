using System.Collections.Generic;
using JumJump.Controller;
using JumJump.Util;
using VContainer;

namespace JumJump.Registry
{
    public sealed class PlatformRegistry
    {
        private readonly List<PlatformController> _platforms = new List<PlatformController>(32);
        private float _roundBottomY;
        private float _roundTopY;
        private bool _hasRoundBounds;

        [Inject]
        public PlatformRegistry()
        {
        }

        public void Register(PlatformController platform)
        {
            if (platform == null || _platforms.Contains(platform))
            {
                return;
            }

            _platforms.Add(platform);
            ExpandRoundBounds(platform);
        }

        public void Unregister(PlatformController platform)
        {
            _platforms.Remove(platform);
        }

        public void Clear()
        {
            for (var i = 0; i < _platforms.Count; i++)
            {
                _platforms[i]?.Release();
            }

            _platforms.Clear();
            _roundBottomY = 0f;
            _roundTopY = 0f;
            _hasRoundBounds = false;
        }

        public void ArchiveBelow(float y)
        {
            for (var i = _platforms.Count - 1; i >= 0; i--)
            {
                var platform = _platforms[i];
                if (platform == null)
                {
                    _platforms.RemoveAt(i);
                    continue;
                }

                if (!platform.gameObject.activeInHierarchy ||
                    platform.CenterY >= y - GameConst.Platform.CleanupYMargin)
                {
                    continue;
                }

                platform.ArchiveForResultView();
            }
        }

        public void ReleaseAbove(float y)
        {
            for (var i = _platforms.Count - 1; i >= 0; i--)
            {
                var platform = _platforms[i];
                if (platform == null)
                {
                    _platforms.RemoveAt(i);
                    continue;
                }

                if (platform.CenterY <= y + GameConst.Platform.CleanupYMargin)
                {
                    continue;
                }

                platform.Release();
            }
        }

        public void ShowAllForResultView()
        {
            for (var i = _platforms.Count - 1; i >= 0; i--)
            {
                var platform = _platforms[i];
                if (platform == null)
                {
                    _platforms.RemoveAt(i);
                    continue;
                }

                platform.ShowForResultView();
            }
        }

        public bool TryGetRoundVerticalBounds(out float bottomY, out float topY)
        {
            bottomY = _roundBottomY;
            topY = _roundTopY;
            return _hasRoundBounds;
        }

        private void ExpandRoundBounds(PlatformController platform)
        {
            if (!_hasRoundBounds)
            {
                _roundBottomY = platform.BottomY;
                _roundTopY = platform.TopY;
                _hasRoundBounds = true;
                return;
            }

            _roundBottomY = UnityEngine.Mathf.Min(_roundBottomY, platform.BottomY);
            _roundTopY = UnityEngine.Mathf.Max(_roundTopY, platform.TopY);
        }
    }
}
