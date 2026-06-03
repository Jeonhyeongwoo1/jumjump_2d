using System;
using JumJump.Event;
using JumJump.Interface;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer.Unity;

namespace JumJump.Service
{
    public sealed class InputActionTapService : IInitializable, IDisposable
    {
        private const string TapActionPath = "Player/Attack";

        private InputAction _tapAction;
        private readonly IEventBus _eventBus;
        private readonly InputActionAsset _inputActions;

        public InputActionTapService(IEventBus eventBus, InputActionAsset inputActions)
        {
            _eventBus = eventBus;
            _inputActions = inputActions;
        }

        public void Initialize()
        {
            if (_inputActions == null)
            {
                Debug.LogError($"[{nameof(InputActionTapService)}] Missing InputActionAsset.");
                return;
            }

            _tapAction = _inputActions.FindAction(TapActionPath, false);
            if (_tapAction == null)
            {
                Debug.LogError($"[{nameof(InputActionTapService)}] Missing input action: {TapActionPath}.");
                return;
            }

            _tapAction.performed += OnTapPerformed;
            _tapAction.Enable();
        }

        public void Dispose()
        {
            if (_tapAction == null)
            {
                return;
            }

            _tapAction.performed -= OnTapPerformed;
            _tapAction.Disable();
        }

        private void OnTapPerformed(InputAction.CallbackContext context)
        {
            _eventBus.Publish(new TapRequestedEvent());
        }
    }
}
