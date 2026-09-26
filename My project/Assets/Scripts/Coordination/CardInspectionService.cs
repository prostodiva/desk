using Cards.Core;
using Zenject;

namespace Cards.Coordination
{
    /// <summary>
    /// Owns the single inspect slot: which card is up, and what the player's
    /// browse/dismiss intents do to it.
    ///
    /// Exactly one card may occupy the anchor. Two cards at identical
    /// positions have coplanar faces that z-fight, which looks like the card
    /// has gone transparent.
    /// </summary>
    public class CardInspectionService : IInitializable, System.IDisposable
    {
        private readonly IInspectAnchor anchor;
        private readonly ICardInspectInput input;
        private readonly ICardProvider provider;

        private CardFacade currentCard;
        private CardBrowser browser;

        public CardInspectionService(
            IInspectAnchor anchor,
            ICardInspectInput input,
            ICardProvider provider)
        {
            this.anchor = anchor;
            this.input = input;
            this.provider = provider;
        }

        public bool IsInspecting => currentCard != null;

        public void Initialize()
        {
            input.NextRequested += OnNext;
            input.PreviousRequested += OnPrevious;
            input.DismissRequested += Dismiss;
            input.FlipRequested += OnFlip;
        }

        public void Dispose()
        {
            input.NextRequested -= OnNext;
            input.PreviousRequested -= OnPrevious;
            input.DismissRequested -= Dismiss;
            input.FlipRequested -= OnFlip;
        }

        /// <summary>Selecting the inspected card sends it home; any other card takes its place.</summary>
        public void Toggle(CardFacade card)
        {
            if (currentCard == card)
                Dismiss();
            else
                Inspect(card);
        }

        public void Inspect(CardFacade card)
        {
            if (card == null || anchor == null) return;

            // Clear the slot first so two cards never share the anchor.
            if (currentCard != null && currentCard != card)
                SendHome(currentCard);

            currentCard = card;

            browser = new CardBrowser(provider.Cards);
            browser.StartFrom(card.Data);
            browser.CardChanged += OnBrowsedToCard;

            card.SetInteractable(false);
            card.Motion.MoveToAnchor(anchor.Transform, () => card.SetInteractable(true));

            input.Enable();
        }

        public void Dismiss()
        {
            if (currentCard == null) return;

            SendHome(currentCard);
            currentCard = null;

            input.Disable();
        }

        /// <summary>
        /// The player picked a card up by hand. If it was the inspected card,
        /// inspection ends right there: the hand now owns its position.
        /// </summary>
        public void OnGrabbed(CardFacade card)
        {
            if (card != currentCard) return;

            StopBrowsing();
            card.RestoreOwnContent();
            currentCard = null;

            input.Disable();
        }

        /// <summary>A held card was let go: it always returns to its slot on the table.</summary>
        public void OnReleased(CardFacade card)
        {
            card.SetInteractable(false);
            card.Motion.MoveHome(() => card.SetInteractable(true));
        }

        private void SendHome(CardFacade card)
        {
            StopBrowsing();

            // Browsing left it showing another card's content; put its own back.
            card.RestoreOwnContent();

            card.SetInteractable(false);
            card.Motion.MoveHome(() => card.SetInteractable(true));
        }

        private void StopBrowsing()
        {
            if (browser == null) return;

            browser.CardChanged -= OnBrowsedToCard;
            browser = null;
        }

        private void OnNext() => browser?.Next();
        private void OnPrevious() => browser?.Previous();
        private void OnFlip() => currentCard?.Motion.Flip();

        private void OnBrowsedToCard(CardData data)
        {
            currentCard?.ShowTemporary(data);
        }
    }
}
