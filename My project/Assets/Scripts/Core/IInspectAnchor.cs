using UnityEngine;

namespace Cards.Core
{
    /// <summary>
    /// The pose a card flies to when inspected.
    /// </summary>
    public interface IInspectAnchor
    {
        Transform Transform { get; }
    }
}
