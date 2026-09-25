using System;
using System.Collections.Generic;

namespace Cards.Core
{
    /// <summary>
    /// Wrapping index over a set of cards. Plain C#, no Unity types — this is
    /// the part worth unit testing, and it stays testable because it knows
    /// nothing about transforms, input or rendering.
    /// </summary>
    public sealed class CardBrowser
    {
        private readonly IReadOnlyList<CardData> cards;
        private int index;

        public event Action<CardData> CardChanged;

        public CardBrowser(IReadOnlyList<CardData> cards)
        {
            this.cards = cards ?? throw new ArgumentNullException(nameof(cards));
        }

        public int Count => cards.Count;
        public bool IsEmpty => cards.Count == 0;
        public CardData Current => IsEmpty ? null : cards[index];

        /// <summary>Starts browsing from a specific card, if it's in the set.</summary>
        public void StartFrom(CardData card)
        {
            for (int i = 0; i < cards.Count; i++)
            {
                if (cards[i] == card)
                {
                    index = i;
                    return;
                }
            }

            index = 0;
        }

        public void Next() => Step(1);
        public void Previous() => Step(-1);

        private void Step(int direction)
        {
            if (cards.Count < 2) return;

            index = (index + direction + cards.Count) % cards.Count;
            CardChanged?.Invoke(Current);
        }
    }
}
