using System;
using JumJump.Data;
using JumJump.Event;
using JumJump.Interface;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer.Unity;

namespace JumJump.Service
{
    public sealed class InputActionTapService : IInitializable, IDisposable
    {
        private InputAction _tapAction;
        private readonly IEventBus _eventBus;
        private readonly InputActionAsset _inputActions;
        private readonly GameConfigData _configData;

        public InputActionTapService(IEventBus eventBus, InputActionAsset inputActions, GameConfigData configData)
        {
            _eventBus = eventBus;
            _inputActions = inputActions;
            _configData = configData;
        }

        public void Initialize()
        {
            if (_inputActions == null)
            {
                Debug.LogError($"[{nameof(InputActionTapService)}] Missing InputActionAsset.");
                return;
            }

            _tapAction = _inputActions.FindAction(_configData.TapActionPath, false);
            if (_tapAction == null)
            {
                Debug.LogError($"[{nameof(InputActionTapService)}] Missing input action: {_configData.TapActionPath}.");
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
