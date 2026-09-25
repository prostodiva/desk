using Cards.Core;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// A physical card the player can point at and select. Selecting it is what
/// CardInspector listens for — the inspect pose, browsing and the return trip
/// all live there, so this script only holds the card's data and its visuals.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class InteractableCard : XRGrabInteractable
{
    [Header("Card Data")]
    [Tooltip("Which card this object represents. Set at runtime by CardStackBuilder.")]
    [SerializeField] private CardData data;

    [Tooltip("Optional. If assigned, thumbnail/title/model update automatically when data is set.")]
    [SerializeField] private CardVisual visual;

    [Header("Hover Feedback (optional)")]
    [Tooltip("Optional child object (glow, outline) shown while a controller points at this card.")]
    [SerializeField] private GameObject hoverHighlight;

    public CardData Data => data;

    protected override void Awake()
    {
        base.Awake();

        // Eases the card in instead of snapping when selected from a distance.
        if (attachEaseInTime <= 0f)
            attachEaseInTime = 0.25f;

        if (data != null)
            SetData(data);
    }

    public void SetData(CardData newData)
    {
        data = newData;
        if (visual != null)
            visual.Bind(data);
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
    }
}
