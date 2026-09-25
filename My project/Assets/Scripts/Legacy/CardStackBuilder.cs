using Cards.Core;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spawns one physical card per entry in a CardDeck, arranges them as a draw
/// pile or a fan, and manages which cards are currently grabbable. Attach to
/// an empty "CardStack" GameObject parented under the table.
/// </summary>
public class CardStackBuilder : MonoBehaviour
{
    public enum LayoutMode { DrawPile, Fan }

    [Header("Data Source")]
    [SerializeField] private CardDeck cardDeck;

    [Header("Card Prefab")]
    [Tooltip("Prefab with InteractableCard + CardVisual + CardInspector. One instance per card in the deck.")]
    [SerializeField] private InteractableCard cardPrefab;

    [Header("Layout")]
    [SerializeField] private LayoutMode layoutMode = LayoutMode.Fan;

    [Header("Draw Pile Settings")]
    [SerializeField] private float stackSpacing = 0.003f;

    [Header("Fan Settings")]
    [SerializeField] private float fanSpacing = 0.05f;
    [SerializeField] private float fanArcAngle = 30f;
    [SerializeField] private bool useArc = true;

    private readonly List<InteractableCard> spawnedCards = new List<InteractableCard>();

    /// <summary>
    /// The deck this stack was built from. CardInspector reads this so the
    /// player can page through the whole deck while inspecting one card.
    /// </summary>
    public CardDeck Deck => cardDeck;

    private static CardStackBuilder instance;

    public static CardStackBuilder Instance
    {
        get
        {
            if (instance == null)
                instance = FindFirstObjectByType<CardStackBuilder>();
            return instance;
        }
    }

    private void Awake()
    {
        instance = this;
        BuildStack();
    }

    /// <summary>
    /// Rebuilds the whole stack from scratch: every card back in its slot
    /// with its own content. Also useful as a "reset everything" call.
    /// </summary>
    [ContextMenu("Rebuild Stack")]
    public void BuildStack()
    {
        ClearSpawnedCards();

        if (cardDeck == null || cardPrefab == null)
        {
            Debug.LogWarning($"{name}: CardStackBuilder needs both a Card Deck and a Card Prefab assigned.", this);
            return;
        }

        foreach (CardData data in cardDeck.Cards)
        {
            InteractableCard card = Instantiate(cardPrefab, transform);
            card.SetData(data);
            spawnedCards.Add(card);
        }

        ArrangeCards();
        WireUpDrawPileEvents();
    }

    private void ClearSpawnedCards()
    {
        foreach (var card in spawnedCards)
            if (card != null) Destroy(card.gameObject);
        spawnedCards.Clear();
    }

    private void ArrangeCards()
    {
        if (spawnedCards.Count == 0) return;

        if (layoutMode == LayoutMode.DrawPile)
            ArrangeAsDrawPile();
        else
            ArrangeAsFan();
    }

    private void ArrangeAsDrawPile()
    {
        for (int i = 0; i < spawnedCards.Count; i++)
        {
            var card = spawnedCards[i];
            if (card == null) continue;

            card.transform.localPosition = new Vector3(0f, stackSpacing * i, 0f);
            card.transform.localRotation = Quaternion.identity;

            // Only the top card is grabbable; the rest stay inert until exposed.
            SetCardInteractable(card, i == spawnedCards.Count - 1);
        }
    }

    private void ArrangeAsFan()
    {
        int count = spawnedCards.Count;
        for (int i = 0; i < count; i++)
        {
            var card = spawnedCards[i];
            if (card == null) continue;

            float t = count > 1 ? (float)i / (count - 1) : 0.5f;
            float angle = Mathf.Lerp(-fanArcAngle * 0.5f, fanArcAngle * 0.5f, t);

            Vector3 localPos;
            Quaternion localRot;

            if (useArc)
            {
                float radius = fanSpacing * count * 0.5f;
                Vector3 baseOffset = new Vector3(0f, radius, 0f);
                localPos = Quaternion.Euler(0f, 0f, angle) * baseOffset - baseOffset;
                localRot = Quaternion.Euler(0f, 0f, angle);
            }
            else
            {
                float x = (i - (count - 1) * 0.5f) * fanSpacing;
                localPos = new Vector3(x, 0f, 0f);
                localRot = Quaternion.identity;
            }

            // Tiny stagger so coplanar colliders don't fight over the raycast.
            localPos += new Vector3(0f, 0.0005f * i, 0f);

            card.transform.localPosition = localPos;
            card.transform.localRotation = localRot;

            SetCardInteractable(card, true);
        }
    }

    private void SetCardInteractable(InteractableCard card, bool interactable)
    {
        card.enabled = interactable;

        var col = card.GetComponent<Collider>();
        if (col != null) col.enabled = interactable;

        var rb = card.GetComponent<Rigidbody>();
        if (rb != null && !interactable)
            rb.isKinematic = true;
    }

    private void WireUpDrawPileEvents()
    {
        if (layoutMode != LayoutMode.DrawPile) return;

        foreach (var card in spawnedCards)
        {
            if (card == null) continue;
            var capturedCard = card;
            card.selectEntered.AddListener(_ => OnCardDrawn(capturedCard));
        }
    }

    /// <summary>
    /// A draw-pile card was taken: expose the next one down.
    /// </summary>
    private void OnCardDrawn(InteractableCard card)
    {
        if (!spawnedCards.Remove(card)) return;

        if (spawnedCards.Count > 0)
            SetCardInteractable(spawnedCards[spawnedCards.Count - 1], true);
    }
}
