using System.Collections.Generic;
using JumJump.Controller;

namespace JumJump.Registry
{
    public sealed class PlatformRegistry
    {
        private readonly List<PlatformController> _platforms = new List<PlatformController>(32);

        public void Register(PlatformController platform)
        {
            if (platform == null || _platforms.Contains(platform))
            {
                return;
            }

            _platforms.Add(platform);
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
        }

        public void ReleaseBelow(float y)
        {
            for (var i = _platforms.Count - 1; i >= 0; i--)
            {
                var platform = _platforms[i];
                if (platform == null)
                {
                    _platforms.RemoveAt(i);
                    continue;
                }

                if (platform.CenterY >= y - 0.05f)
                {
                    continue;
                }

                platform.Release();
                _platforms.RemoveAt(i);
            }
        }
    }
}
