using System;
using Cards.Core;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace Cards.Adapters
{
    /// <summary>
    /// Wraps XRI's grab interactable behind ICardSelectable. This is the only
    /// type in the card system that references XR Interaction Toolkit, so
    /// swapping input stacks (hands, a flat-screen build, automated tests)
    /// means replacing this one adapter.
    ///
    /// XRI Select (grip / hand grab) holds the card physically; XRI Activate
    /// (trigger) asks to inspect it. For the trigger to reach a card that
    /// isn't held, enable "Allow Hovered Activate" on the controllers'
    /// Near-Far Interactor.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class XRCardSelectable : XRGrabInteractable, ICardSelectable
    {
        [Header("Hover Feedback (optional)")]
        [SerializeField] private GameObject hoverHighlight;

        public event Action InspectRequested;
        public event Action Grabbed;
        public event Action Released;

        public bool InteractionEnabled
        {
            get => enabled;
            set
            {
                enabled = value;

                var col = GetComponent<Collider>();
                if (col != null) col.enabled = value;

                if (!value)
                {
                    var rb = GetComponent<Rigidbody>();
                    if (rb != null) rb.isKinematic = true;
                }
            }
        }

        protected override void Awake()
        {
            base.Awake();

            if (attachEaseInTime <= 0f)
                attachEaseInTime = 0.25f;

            // A released card always flies back to its slot, so a throw would
            // only fight that animation.
            throwOnDetach = false;
        }

        protected override void OnHoverEntered(HoverEnterEventArgs args)
        {
            base.OnHoverEntered(args);
            if (hoverHighlight != null) hoverHighlight.SetActive(true);
        }

        protected override void OnHoverExited(HoverExitEventArgs args)
        {
            base.OnHoverExited(args);
            if (hoverHighlight != null) hoverHighlight.SetActive(false);
        }

        protected override void OnSelectEntered(SelectEnterEventArgs args)
        {
            base.OnSelectEntered(args);
            if (hoverHighlight != null) hoverHighlight.SetActive(false);
            Grabbed?.Invoke();
        }

        protected override void OnSelectExited(SelectExitEventArgs args)
        {
            base.OnSelectExited(args);
            Released?.Invoke();
        }

        protected override void OnActivated(ActivateEventArgs args)
        {
            base.OnActivated(args);

            // Trigger while holding the card is ignored: flying it to the
            // inspect pose from inside the player's hand would fight the grab.
            if (!isSelected) InspectRequested?.Invoke();
        }
    }
}
