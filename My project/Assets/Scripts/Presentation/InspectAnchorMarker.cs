using Cards.Core;
using UnityEngine;

namespace Cards.Presentation
{
    /// <summary>
    /// Marks where a card sits while being inspected. Put this on an empty
    /// GameObject parented under the XR Origin's Main Camera, then rotate it
    /// until the gizmo card faces the player.
    /// </summary>
    public class InspectAnchorMarker : MonoBehaviour, IInspectAnchor
    {
        public Transform Transform => transform;

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(0.15f, 0.002f, 0.21f));
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(Vector3.zero, Vector3.up * 0.1f);
        }
#endif
    }
}
