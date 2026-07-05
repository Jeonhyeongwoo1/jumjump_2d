using System;
using System.Collections.Generic;
using JumJump.Data;
using JumJump.Event;
using JumJump.Interface;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using VContainer;
using VContainer.Unity;

namespace JumJump.Service
{
    public sealed class InputActionTapService : IInitializable, IDisposable
    {
        private InputAction _tapAction;
        private InputAction _jumpAction;
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

            _jumpAction = _inputActions.FindAction(_configData.JumpActionPath, true);
            if (_jumpAction != _tapAction)
            {
                _jumpAction.performed += OnTapPerformed;
                _jumpAction.Enable();
            }
        }

        public void Dispose()
        {
            _tapAction.performed -= OnTapPerformed;
            _tapAction.Disable();

            if (_jumpAction != _tapAction)
            {
                _jumpAction.performed -= OnTapPerformed;
                _jumpAction.Disable();
            }
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
            return HasBlockingUiRaycast();
        }

        private bool HasBlockingUiRaycast()
        {
            for (var i = 0; i < _uiRaycastResults.Count; i++)
            {
                if (IsBlockingUiObject(_uiRaycastResults[i].gameObject))
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsBlockingUiObject(GameObject gameObject)
        {
            if (gameObject == null)
            {
                return false;
            }

            var current = gameObject.transform;
            while (current != null)
            {
                if (current.TryGetComponent<Selectable>(out var selectable) &&
                    selectable.IsActive() &&
                    selectable.IsInteractable())
                {
                    return true;
                }

                if (current.TryGetComponent<ScrollRect>(out var scrollRect) &&
                    scrollRect.enabled &&
                    scrollRect.IsActive())
                {
                    return true;
                }

                current = current.parent;
            }

            return false;
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
