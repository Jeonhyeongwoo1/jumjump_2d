using System;
using System.Collections;
using UnityEngine;

namespace JumJump.Controller
{
    public sealed class CoinCollectFX : MonoBehaviour
    {
        [SerializeField] private ParticleSystem _glowRing;
        [SerializeField] private ParticleSystem _starBurst;
        [SerializeField] private ParticleSystem _sparkleBurst;
        [SerializeField] private ParticleSystem _floatingDots;
        [SerializeField] private ParticleSystem _coinPop;
        [SerializeField] private bool _autoDestroy = true;
        [SerializeField] private bool _autoDisable;
        [SerializeField] private float _destroyDelay = 0.8f;

        private Action<CoinCollectFX> _onReturnedToPool = _ => { };
        private Coroutine _playRoutine;
        private bool _hasPoolBinding;
        private bool _isActive;

        public void Bind(Action<CoinCollectFX> onReturnedToPool)
        {
            _onReturnedToPool = onReturnedToPool;
            _hasPoolBinding = true;
        }

        public void Play(Vector3 worldPosition)
        {
            if (_playRoutine != null)
            {
                StopCoroutine(_playRoutine);
                _playRoutine = null;
            }

            transform.position = worldPosition;
            gameObject.SetActive(true);
            _isActive = true;
            StopAndClear();
            _playRoutine = StartCoroutine(PlayRoutine());
        }

        public void StopAndClear()
        {
            StopParticle(_glowRing);
            StopParticle(_starBurst);
            StopParticle(_sparkleBurst);
            StopParticle(_floatingDots);
            StopParticle(_coinPop);
        }

        public void SetPooled()
        {
            _isActive = false;
            if (_playRoutine != null)
            {
                StopCoroutine(_playRoutine);
                _playRoutine = null;
            }

            StopAndClear();
            gameObject.SetActive(false);
        }

        private IEnumerator PlayRoutine()
        {
            RestartParticle(_glowRing);
            RestartParticle(_starBurst);
            RestartParticle(_coinPop);

            yield return new WaitForSeconds(0.03f);
            RestartParticle(_sparkleBurst);

            yield return new WaitForSeconds(0.05f);
            RestartParticle(_floatingDots);

            yield return new WaitForSeconds(Mathf.Max(0f, _destroyDelay - 0.08f));
            _playRoutine = null;
            CompletePlayback();
        }

        private void CompletePlayback()
        {
            if (!_isActive)
            {
                return;
            }

            _isActive = false;
            if (_hasPoolBinding)
            {
                _onReturnedToPool.Invoke(this);
                return;
            }

            if (_autoDestroy)
            {
                Destroy(gameObject);
                return;
            }

            if (_autoDisable)
            {
                gameObject.SetActive(false);
            }
        }

        private void RestartParticle(ParticleSystem particleSystem)
        {
            if (particleSystem == null)
            {
                return;
            }

            particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            particleSystem.Play(true);
        }

        private void StopParticle(ParticleSystem particleSystem)
        {
            if (particleSystem == null)
            {
                return;
            }

            particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        private void OnDisable()
        {
            if (_playRoutine != null)
            {
                StopCoroutine(_playRoutine);
                _playRoutine = null;
            }

            StopAndClear();
        }
    }
}
