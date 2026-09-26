using Cards.Core;
using UnityEngine;
using Zenject;

namespace Cards.Coordination
{
    /// <summary>
    /// One spawned card, as Coordination sees it: something selectable, with
    /// a view and a way to move. The three parts are injected from the
    /// components on the prefab, so this type never does GetComponent lookups
    /// or knows which concrete classes are involved.
    /// </summary>
    public class CardFacade : MonoBehaviour
    {
        private ICardSelectable selectable;
        private ICardView view;
        private ICardMotion motion;
        private CardInspectionService inspection;

        public CardData Data { get; private set; }
        public ICardMotion Motion => motion;

        [Inject]
        public void Construct(
            ICardSelectable selectable,
            ICardView view,
            ICardMotion motion,
            CardInspectionService inspection)
        {
            this.selectable = selectable;
            this.view = view;
            this.motion = motion;
            this.inspection = inspection;
        }

        private void OnEnable()
        {
            selectable.InspectRequested += OnInspectRequested;
            selectable.Grabbed += OnGrabbed;
            selectable.Released += OnReleased;
        }

        private void OnDisable()
        {
            selectable.InspectRequested -= OnInspectRequested;
            selectable.Grabbed -= OnGrabbed;
            selectable.Released -= OnReleased;
        }

        private void OnInspectRequested() => inspection.Toggle(this);
        private void OnGrabbed() => inspection.OnGrabbed(this);
        private void OnReleased() => inspection.OnReleased(this);

        /// <summary>Assigns this card's own content and renders it.</summary>
        public void SetData(CardData data)
        {
            Data = data;
            view.Bind(data);
        }

        /// <summary>
        /// Shows another card's content without changing what this card IS.
        /// Used while browsing, so Data still points at the card's own entry
        /// and it can be restored on the way home.
        /// </summary>
        public void ShowTemporary(CardData data) => view.Bind(data);

        public void RestoreOwnContent() => view.Bind(Data);

        public void SetInteractable(bool value) => selectable.InteractionEnabled = value;

        public class Factory : PlaceholderFactory<CardFacade> { }
    }
}
