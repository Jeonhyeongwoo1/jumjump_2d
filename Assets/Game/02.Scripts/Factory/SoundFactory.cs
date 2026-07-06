using UnityEngine;
using VContainer;

namespace JumJump.Factory
{
    public sealed class SoundFactory
    {
        [Inject]
        public SoundFactory()
        {
        }

        public Transform CreateRoot()
        {
            var root = new GameObject("SoundRoot");
            root.AddComponent<AudioListener>();
            return root.transform;
        }

        public AudioSource CreateAudioSource(Transform root, string name, bool loop)
        {
            var gameObject = new GameObject(name);
            gameObject.transform.SetParent(root, false);

            var source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = loop;
            source.spatialBlend = 0f;
            source.dopplerLevel = 0f;
            return source;
        }
    }
}
