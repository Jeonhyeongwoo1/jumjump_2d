using System;
using AppsInToss;
using Cysharp.Threading.Tasks;
using JumJump.Event;
using JumJump.Interface;
using JumJump.Util;
using VContainer;
using VContainer.Unity;

namespace JumJump.Service
{
    public sealed class HapticFeedbackService : IInitializable, IDisposable
    {
        private readonly IEventBus _eventBus;
        private bool _hasLoggedFailure;

        [Inject]
        public HapticFeedbackService(IEventBus eventBus)
        {
            _eventBus = eventBus;
        }

        public void Initialize()
        {
            _eventBus.Subscribe<PlayerJumpStartedEvent>(OnPlayerJumpStarted);
            _eventBus.Subscribe<HapticFeedbackRequestedEvent>(OnHapticFeedbackRequested);
        }

        public void Dispose()
        {
            _eventBus.Unsubscribe<PlayerJumpStartedEvent>(OnPlayerJumpStarted);
            _eventBus.Unsubscribe<HapticFeedbackRequestedEvent>(OnHapticFeedbackRequested);
        }

        private void OnPlayerJumpStarted(in PlayerJumpStartedEvent ev)
        {
            PlayLight();
        }

        private void OnHapticFeedbackRequested(in HapticFeedbackRequestedEvent ev)
        {
            PlayLight();
        }

        private void PlayLight()
        {
            PlayLightAsync().Forget();
        }

        private async UniTask PlayLightAsync()
        {
            try
            {
                await AIT.GenerateHapticFeedback(new HapticFeedbackOptions
                {
                    Type = HapticFeedbackType.Tap
                });
            }
            catch (Exception ex)
            {
                if (_hasLoggedFailure)
                {
                    return;
                }

                _hasLoggedFailure = true;
                GameLogger.Warning(nameof(HapticFeedbackService), $"haptic_feedback_failed: {ex.Message}");
            }
        }
    }
}
