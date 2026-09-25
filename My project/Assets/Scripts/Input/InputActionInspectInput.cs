using System;
using Cards.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Cards.Input
{
    /// <summary>
    /// Turns raw Input System actions into inspect intents. This is the only
    /// place that knows about thumbstick deadzones or which button dismisses —
    /// Coordination just hears "next", "previous", "dismiss".
    ///
    /// Put this on the XR rig and bind it in the installer.
    /// </summary>
    public class InputActionInspectInput : MonoBehaviour, ICardInspectInput
    {
        [Header("Browsing")]
        [Tooltip("Vector2 action used to page through the deck — normally a thumbstick.")]
        [SerializeField] private InputActionReference browseInput;

        [Range(0.2f, 0.95f)]
        [SerializeField] private float browseThreshold = 0.6f;

        [Header("Dismiss")]
        [Tooltip("Button action that sends the inspected card back to the table.")]
        [SerializeField] private InputActionReference dismissInput;

        public event Action NextRequested;
        public event Action PreviousRequested;
        public event Action DismissRequested;

        private bool listening;
        private bool stickCentered = true;

        public void Enable()
        {
            listening = true;
            stickCentered = true;

            if (browseInput != null) browseInput.action.Enable();

            if (dismissInput != null)
            {
                dismissInput.action.Enable();
                dismissInput.action.performed += OnDismiss;
            }
        }

        public void Disable()
        {
            listening = false;

            if (dismissInput != null)
                dismissInput.action.performed -= OnDismiss;
        }

        private void OnDismiss(InputAction.CallbackContext _)
        {
            if (listening) DismissRequested?.Invoke();
        }

        private void Update()
        {
            if (!listening || browseInput == null) return;

            float x = browseInput.action.ReadValue<Vector2>().x;

            // The stick must return to centre between pages, so one flick
            // moves one card instead of scrolling the whole deck.
            if (Mathf.Abs(x) < 0.2f)
            {
                stickCentered = true;
                return;
            }

            if (!stickCentered || Mathf.Abs(x) < browseThreshold) return;

            stickCentered = false;

            if (x > 0f) NextRequested?.Invoke();
            else PreviousRequested?.Invoke();
        }
    }
}
