using System.Collections.Generic;

namespace Cards.Core
{
    /// <summary>
    /// Source of an ordered set of cards. Coordination depends on this rather
    /// than on CardDeck, so a deck can be swapped for a remote-loaded set or
    /// a test double without touching anything downstream.
    /// </summary>
    public interface ICardProvider
    {
        IReadOnlyList<CardData> Cards { get; }
    }
}
