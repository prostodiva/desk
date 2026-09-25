using System.Collections.Generic;
using UnityEngine;

namespace Cards.Core
{
    /// <summary>
    /// An authored deck asset. The default ICardProvider implementation.
    /// </summary>
    [CreateAssetMenu(fileName = "NewCardDeck", menuName = "Cards/Card Deck")]
    public class CardDeck : ScriptableObject, ICardProvider
    {
        [SerializeField] private List<CardData> cards = new List<CardData>();

        public IReadOnlyList<CardData> Cards => cards;
    }
}
