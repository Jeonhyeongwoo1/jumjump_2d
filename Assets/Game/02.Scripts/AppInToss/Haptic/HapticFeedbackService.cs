using System;
using JumJump.Bridge;
using JumJump.Event;
using JumJump.Interface;
using VContainer;
using VContainer.Unity;

namespace JumJump.Service
{
    public sealed class HapticFeedbackService : IInitializable, IDisposable
    {
        private readonly IEventBus _eventBus;

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
            AppInTossHapticWebGL.VibrateLight();
        }
    }
}
