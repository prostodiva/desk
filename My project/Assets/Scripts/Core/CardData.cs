using UnityEngine;
using UnityEngine.Serialization;

namespace Cards.Core
{
    /// <summary>
    /// Content for a single card. Pure data — no behaviour, no scene
    /// references, no knowledge of how it gets displayed.
    /// </summary>
    [CreateAssetMenu(fileName = "NewCard", menuName = "Cards/Card Data")]
    public class CardData : ScriptableObject
    {
        [SerializeField] private string title;
        [SerializeField] private GameObject modelPrefab;
        [FormerlySerializedAs("thumbnail2D")]
        [SerializeField] private Sprite thumbnail;

        [Tooltip("Shown on the back of the card.")]
        [TextArea(3, 10)]
        [SerializeField] private string description;

        public string Title => title;
        public string Description => description;
        public GameObject ModelPrefab => modelPrefab;
        public Sprite Thumbnail => thumbnail;
    }
}
