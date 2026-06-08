using System;
using UnityEngine;

namespace JumJump.Service
{
    public sealed class HayLandingFX : MonoBehaviour
    {
        [SerializeField] private AutoDisableLifecycle _lifecycle;
        [SerializeField] private ParticleSystem _glowDots;
        [SerializeField] private ParticleSystem _dustPuff;
        [SerializeField] private ParticleSystem _twinklePerfect;

        private Action<HayLandingFX> _onReturnedToPool = _ => { };
        private bool _isActive;

        public void Bind(Action<HayLandingFX> onReturnedToPool)
        {
            _onReturnedToPool = onReturnedToPool;
            _lifecycle.Configure(_lifecycle.Duration, Deactivate, OnDisabled);
        }

        public void Play(Vector3 position, bool includePerfectTwinkle)
        {
            _isActive = true;
            transform.position = position;
            StopAll();
            gameObject.SetActive(true);
            Restart(_glowDots);
            Restart(_dustPuff);
            _lifecycle.Begin();

            if (includePerfectTwinkle)
            {
                Restart(_twinklePerfect);
                return;
            }

            _twinklePerfect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        public void Deactivate()
        {
            if (!_isActive && !gameObject.activeSelf)
            {
                return;
            }

            _lifecycle.Stop();
            StopAll();
            gameObject.SetActive(false);
        }

        public void SetPooled()
        {
            _isActive = false;
            _lifecycle.Stop();
            StopAll();
            gameObject.SetActive(false);
        }

        private void OnDisabled()
        {
            if (!_isActive)
            {
                return;
            }

            _isActive = false;
            _onReturnedToPool.Invoke(this);
        }

        private void StopAll()
        {
            _glowDots.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            _dustPuff.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            _twinklePerfect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        private static void Restart(ParticleSystem particleSystem)
        {
            particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            particleSystem.Play(true);
        }
    }
}
