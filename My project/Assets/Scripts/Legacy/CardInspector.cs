using Cards.Core;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// Click a card and it flies to a fixed, hands-free inspect pose in front of
/// the player. While it's there, flick the thumbstick left/right to page
/// through the whole deck without going back to the table. Click it again and
/// it returns to the stack.
///
/// The pose comes from a CardInspectAnchor in the scene: the card parents to
/// it at zero local position/rotation, so it inherits that orientation exactly.
///
/// Browsing swaps the card's CONTENT, not the object — the same physical card
/// rebinds to the next CardData, so nothing flies across the room mid-swap.
/// </summary>
[RequireComponent(typeof(InteractableCard))]
public class CardInspector : MonoBehaviour
{
    [Header("Motion")]
    [SerializeField] private float travelDuration = 0.35f;

    [Header("Browsing")]
    [Tooltip("Vector2 action used to page through the deck while inspecting — normally the thumbstick (e.g. XRI Left Interaction/Manipulation, or a dedicated action).")]
    [SerializeField] private InputActionReference browseInput;

    [Tooltip("How far the stick must be pushed before it counts as a page turn.")]
    [Range(0.2f, 0.95f)]
    [SerializeField] private float browseThreshold = 0.6f;

    [Tooltip("Small pop when the card's content changes, so the swap reads as a change.")]
    [SerializeField] private bool pulseOnSwap = true;

    [Header("Dismiss")]
    [Tooltip("Optional button action that sends the inspected card back to the table — e.g. the grip or a face button. Clicking the card also works.")]
    [SerializeField] private InputActionReference dismissInput;

    [Tooltip("Put the card's own content back when it returns, instead of leaving it showing whatever you browsed to.")]
    [SerializeField] private bool restoreOwnCardOnReturn = true;

    public bool IsInspecting { get; private set; }

    /// <summary>
    /// The card currently at the inspect anchor, if any. Only one card may
    /// occupy the anchor — otherwise two cards land at identical positions
    /// and their coplanar faces z-fight, which looks like transparency.
    /// </summary>
    private static CardInspector current;

    private InteractableCard card;
    private Rigidbody rb;

    private Transform homeParent;
    private Vector3 homeLocalPosition;
    private Quaternion homeLocalRotation;

    private Coroutine motionRoutine;

    // Browsing state
    private IReadOnlyList<CardData> deckCards;
    private int browseIndex;
    private bool stickWasCentered = true;
    private CardData ownData;

    private void Awake()
    {
        card = GetComponent<InteractableCard>();
        rb = GetComponent<Rigidbody>();
    }

    private void OnEnable()
    {
        card.selectEntered.AddListener(OnSelected);
        if (browseInput != null) browseInput.action.Enable();
        if (dismissInput != null)
        {
            dismissInput.action.Enable();
            dismissInput.action.performed += OnDismissPressed;
        }
    }

    private void OnDisable()
    {
        card.selectEntered.RemoveListener(OnSelected);
        if (browseInput != null) browseInput.action.Disable();
        if (dismissInput != null)
        {
            dismissInput.action.performed -= OnDismissPressed;
            dismissInput.action.Disable();
        }
    }

    private void OnDismissPressed(InputAction.CallbackContext _)
    {
        if (IsInspecting) SendHome();
    }

    /// <summary>
    /// Sends whatever card is currently being inspected back to the table.
    /// Call from a UI button, another script, or a scene event.
    /// </summary>
    public static void DismissCurrent()
    {
        if (current != null) current.SendHome();
    }

    private void OnSelected(SelectEnterEventArgs args)
    {
        if (IsInspecting)
            SendHome();
        else
            BeginInspect();
    }

    private void BeginInspect()
    {
        Transform anchor = CardInspectAnchor.Instance != null
            ? CardInspectAnchor.Instance.transform
            : null;

        if (anchor == null)
        {
            Debug.LogWarning($"{name}: no CardInspectAnchor found in the scene. " +
                             "Add one under the Main Camera.", this);
            return;
        }

        homeParent = transform.parent;
        homeLocalPosition = transform.localPosition;
        homeLocalRotation = transform.localRotation;

        // Clear the anchor first so two cards never share it.
        if (current != null && current != this)
            current.SendHome();

        current = this;

        ownData = card.Data;

        ResolveDeck();

        IsInspecting = true;
        stickWasCentered = true;

        card.enabled = false;

        if (motionRoutine != null) StopCoroutine(motionRoutine);
        motionRoutine = StartCoroutine(MoveToAnchor(anchor));
    }

    /// <summary>
    /// Finds the deck this card belongs to and where in it this card sits, so
    /// browsing starts from the card the player actually picked up.
    /// </summary>
    private void ResolveDeck()
    {
        deckCards = null;
        browseIndex = 0;

        var builder = CardStackBuilder.Instance;
        if (builder == null || builder.Deck == null) return;

        deckCards = builder.Deck.Cards;
        if (deckCards == null || deckCards.Count == 0) return;

        for (int i = 0; i < deckCards.Count; i++)
        {
            if (deckCards[i] == card.Data)
            {
                browseIndex = i;
                return;
            }
        }
    }

    private void SendHome()
    {
        IsInspecting = false;
        card.enabled = false;

        if (current == this)
            current = null;

        if (motionRoutine != null) StopCoroutine(motionRoutine);
        motionRoutine = StartCoroutine(MoveHome());
    }

    private void Update()
    {
        if (!IsInspecting || browseInput == null) return;
        if (deckCards == null || deckCards.Count < 2) return;

        float x = browseInput.action.ReadValue<Vector2>().x;

        // Require the stick to return to centre between pages, so one flick
        // moves one card instead of scrolling the whole deck.
        if (Mathf.Abs(x) < 0.2f)
        {
            stickWasCentered = true;
            return;
        }

        if (!stickWasCentered || Mathf.Abs(x) < browseThreshold) return;

        Step(x > 0f ? 1 : -1);
        stickWasCentered = false;
    }

    private void Step(int direction)
    {
        browseIndex = (browseIndex + direction + deckCards.Count) % deckCards.Count;
        card.SetData(deckCards[browseIndex]);

        if (pulseOnSwap)
            StartCoroutine(PulseRoutine());
    }

    /// <summary>
    /// Quick scale bump so a content change is visible even when two cards
    /// look broadly similar.
    /// </summary>
    private IEnumerator PulseRoutine()
    {
        Vector3 baseScale = transform.localScale;
        const float duration = 0.12f;

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float p = t / duration;
            float bump = 1f + 0.08f * Mathf.Sin(p * Mathf.PI);
            transform.localScale = baseScale * bump;
            yield return null;
        }

        transform.localScale = baseScale;
    }

    private IEnumerator MoveToAnchor(Transform anchor)
    {
        FreezePhysics();

        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;
        transform.SetParent(anchor, worldPositionStays: true);

        float t = 0f;
        while (t < travelDuration)
        {
            t += Time.deltaTime;
            float eased = Mathf.SmoothStep(0f, 1f, t / travelDuration);

            transform.localPosition = Vector3.Lerp(
                anchor.InverseTransformPoint(startPos), Vector3.zero, eased);
            transform.localRotation = Quaternion.Slerp(
                Quaternion.Inverse(anchor.rotation) * startRot, Quaternion.identity, eased);

            yield return null;
        }

        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;

        card.enabled = true;
        motionRoutine = null;
    }

    private IEnumerator MoveHome()
    {
        FreezePhysics();

        // Browsing left this object showing another card's content; put its
        // own back so the stack stays consistent.
        if (restoreOwnCardOnReturn && ownData != null && card.Data != ownData)
            card.SetData(ownData);

        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;

        transform.SetParent(homeParent, worldPositionStays: true);

        Vector3 targetPos = homeParent != null
            ? homeParent.TransformPoint(homeLocalPosition)
            : homeLocalPosition;
        Quaternion targetRot = homeParent != null
            ? homeParent.rotation * homeLocalRotation
            : homeLocalRotation;

        float t = 0f;
        while (t < travelDuration)
        {
            t += Time.deltaTime;
            float eased = Mathf.SmoothStep(0f, 1f, t / travelDuration);
            transform.position = Vector3.Lerp(startPos, targetPos, eased);
            transform.rotation = Quaternion.Slerp(startRot, targetRot, eased);
            yield return null;
        }

        transform.localPosition = homeLocalPosition;
        transform.localRotation = homeLocalRotation;

        card.enabled = true;
        motionRoutine = null;
    }

    private void FreezePhysics()
    {
        if (rb == null) return;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = true;
    }
}
