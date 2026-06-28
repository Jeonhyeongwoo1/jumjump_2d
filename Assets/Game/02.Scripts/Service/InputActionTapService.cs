using System;
using System.Collections.Generic;
using JumJump.Data;
using JumJump.Event;
using JumJump.Interface;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using VContainer;
using VContainer.Unity;

namespace JumJump.Service
{
    public sealed class InputActionTapService : IInitializable, IDisposable
    {
        private InputAction _tapAction;
        private EventSystem _pointerEventSystem;
        private PointerEventData _pointerEventData;
        private readonly List<RaycastResult> _uiRaycastResults = new List<RaycastResult>(8);
        private readonly IEventBus _eventBus;
        private readonly InputActionAsset _inputActions;
        private readonly GameConfigData _configData;

        [Inject]
        public InputActionTapService(IEventBus eventBus, InputActionAsset inputActions, GameConfigData configData)
        {
            _eventBus = eventBus;
            _inputActions = inputActions;
            _configData = configData;
        }

        public void Initialize()
        {
            _tapAction = _inputActions.FindAction(_configData.TapActionPath, true);
            _tapAction.performed += OnTapPerformed;
            _tapAction.Enable();
        }

        public void Dispose()
        {
            _tapAction.performed -= OnTapPerformed;
            _tapAction.Disable();
        }

        private void OnTapPerformed(InputAction.CallbackContext context)
        {
            if (IsPointerOverUi(context))
            {
                return;
            }

            _eventBus.Publish(new TapRequestedEvent());
        }

        private bool IsPointerOverUi(InputAction.CallbackContext context)
        {
            var eventSystem = EventSystem.current;
            if (eventSystem == null || !TryResolvePointerPosition(context, out var pointerPosition))
            {
                return false;
            }

            if (_pointerEventData == null || _pointerEventSystem != eventSystem)
            {
                _pointerEventSystem = eventSystem;
                _pointerEventData = new PointerEventData(eventSystem);
            }

            _pointerEventData.Reset();
            _pointerEventData.position = pointerPosition;
            _uiRaycastResults.Clear();
            eventSystem.RaycastAll(_pointerEventData, _uiRaycastResults);
            return _uiRaycastResults.Count > 0;
        }

        private bool TryResolvePointerPosition(InputAction.CallbackContext context, out Vector2 pointerPosition)
        {
            switch (context.control.device)
            {
                case Touchscreen touchscreen:
                    pointerPosition = touchscreen.primaryTouch.position.ReadValue();
                    return true;
                case Mouse mouse:
                    pointerPosition = mouse.position.ReadValue();
                    return true;
                case Pen pen:
                    pointerPosition = pen.position.ReadValue();
                    return true;
                default:
                    pointerPosition = default;
                    return false;
            }
        }
    }
}
