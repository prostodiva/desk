using System;

namespace Cards.Core
{
    /// <summary>
    /// Player intent while a card is being inspected, expressed as events
    /// rather than raw input. Implemented in the Input layer.
    /// </summary>
    public interface ICardInspectInput
    {
        event Action NextRequested;
        event Action PreviousRequested;
        event Action DismissRequested;
        event Action FlipRequested;

        void Enable();
        void Disable();
    }
}
