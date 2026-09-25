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
        }

        public void Dispose()
        {
            input.NextRequested -= OnNext;
            input.PreviousRequested -= OnPrevious;
            input.DismissRequested -= Dismiss;
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

        private void SendHome(CardFacade card)
        {
            if (browser != null)
            {
                browser.CardChanged -= OnBrowsedToCard;
                browser = null;
            }

            // Browsing left it showing another card's content; put its own back.
            card.RestoreOwnContent();

            card.SetInteractable(false);
            card.Motion.MoveHome(() => card.SetInteractable(true));
        }

        private void OnNext() => browser?.Next();
        private void OnPrevious() => browser?.Previous();

        private void OnBrowsedToCard(CardData data)
        {
            currentCard?.ShowTemporary(data);
        }
    }
}
