using System;
using UnityEngine;

namespace Cards.Core
{
    /// <summary>
    /// Moves a card between its slot and the inspect pose. Implemented in
    /// Presentation so Coordination never touches transforms or coroutines.
    /// </summary>
    public interface ICardMotion
    {
        void MoveToAnchor(Transform anchor, Action onComplete = null);
        void MoveHome(Action onComplete = null);
        void CaptureHome();

        /// <summary>Turns the card over in place. Ignored while it's still moving.</summary>
        void Flip();
    }
}
