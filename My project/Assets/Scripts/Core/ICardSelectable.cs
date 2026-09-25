using System;

namespace Cards.Core
{
    /// <summary>
    /// Something the player can select. Implemented in Adapters by the XRI
    /// interactable, which keeps XR Interaction Toolkit out of every other layer.
    /// </summary>
    public interface ICardSelectable
    {
        event Action Selected;
        bool InteractionEnabled { get; set; }
    }
}
