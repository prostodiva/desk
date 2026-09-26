using System;

namespace Cards.Core
{
    /// <summary>
    /// The player's intents toward one card. Implemented in Adapters by the
    /// XRI interactable, which keeps XR Interaction Toolkit out of every other
    /// layer — which button or gesture produces each intent is the adapter's
    /// business.
    /// </summary>
    public interface ICardSelectable
    {
        /// <summary>The player asked to inspect this card (e.g. point + trigger).</summary>
        event Action InspectRequested;

        /// <summary>The player picked the card up (e.g. grip, or a hand grab).</summary>
        event Action Grabbed;

        /// <summary>The player let go of a card they were holding.</summary>
        event Action Released;

        bool InteractionEnabled { get; set; }
    }
}
