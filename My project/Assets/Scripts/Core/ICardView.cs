namespace Cards.Core
{
    /// <summary>
    /// Renders a card's content. Implemented in Presentation.
    /// </summary>
    public interface ICardView
    {
        void Bind(CardData data);
    }
}
