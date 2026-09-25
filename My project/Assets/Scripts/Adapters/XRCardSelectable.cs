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
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class XRCardSelectable : XRGrabInteractable, ICardSelectable
    {
        [Header("Hover Feedback (optional)")]
        [SerializeField] private GameObject hoverHighlight;

        public event Action Selected;

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
            Selected?.Invoke();
        }
    }
}
