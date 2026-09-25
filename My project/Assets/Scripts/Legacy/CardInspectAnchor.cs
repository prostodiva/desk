using UnityEngine;

/// <summary>
/// Marks the spot a card flies to when inspected. Put this on an empty
/// GameObject parented under your XR Origin's Main Camera, then position and
/// rotate it so a card sitting at its origin faces the player.
///
/// Cards find this at runtime, so nothing needs wiring in the prefab.
/// </summary>
public class CardInspectAnchor : MonoBehaviour
{
    private static CardInspectAnchor instance;

    public static CardInspectAnchor Instance
    {
        get
        {
            if (instance == null)
                instance = FindFirstObjectByType<CardInspectAnchor>();
            return instance;
        }
    }

    private void Awake()
    {
        instance = this;
    }

#if UNITY_EDITOR
    // Draws a card-sized rectangle in the Scene view so you can see the pose
    // you're setting up without spawning a temporary card.
    private void OnDrawGizmos()
    {
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(0.15f, 0.002f, 0.21f));
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(Vector3.zero, Vector3.up * 0.1f); // card's face direction
    }
#endif
}
