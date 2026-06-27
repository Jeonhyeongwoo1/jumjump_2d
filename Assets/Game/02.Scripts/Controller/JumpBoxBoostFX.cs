using System.Collections;
using System;
using UnityEngine;

namespace JumJump.Controller
{
    public sealed class JumpBoxBoostFX : MonoBehaviour
    {
        [SerializeField] private ParticleSystem _impactRing;
        [SerializeField] private ParticleSystem _dustPuff;
        [SerializeField] private ParticleSystem _boostArrows;
        [SerializeField] private ParticleSystem _speedLines;
        [SerializeField] private ParticleSystem _sparkles;
        [SerializeField] private bool _disableOnComplete = true;
        [SerializeField] private bool _destroyOnComplete;
        [SerializeField] private float _completeDelay = 0.8f;

        private Action<JumpBoxBoostFX> _onReturnedToPool = _ => { };
        private Coroutine _playRoutine;
        private bool _hasPoolBinding;
        private bool _isActive;

        public void Bind(Action<JumpBoxBoostFX> onReturnedToPool)
        {
            _onReturnedToPool = onReturnedToPool;
            _hasPoolBinding = true;
        }

        public void Play()
        {
            Play(transform.position);
        }

        public void Play(Vector3 position)
        {
            if (_playRoutine != null)
            {
                StopCoroutine(_playRoutine);
            }

            _isActive = true;
            transform.position = position;
            gameObject.SetActive(true);
            StopAndClear();
            _playRoutine = StartCoroutine(PlayRoutine());
        }

        public void StopAndClear()
        {
            StopParticle(_impactRing);
            StopParticle(_dustPuff);
            StopParticle(_boostArrows);
            StopParticle(_speedLines);
            StopParticle(_sparkles);
        }

        private IEnumerator PlayRoutine()
        {
            RestartParticle(_impactRing);
            RestartParticle(_dustPuff);

            yield return new WaitForSeconds(0.06f);
            RestartParticle(_sparkles);

            yield return new WaitForSeconds(0.06f);
            RestartParticle(_boostArrows);
            RestartParticle(_speedLines);

            yield return new WaitForSeconds(Mathf.Max(0f, _completeDelay));
            _playRoutine = null;
            CompletePlayback();
        }

        private void CompletePlayback()
        {
            if (_destroyOnComplete)
            {
                Destroy(gameObject);
                return;
            }

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

            if (_disableOnComplete)
            {
                gameObject.SetActive(false);
            }
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
