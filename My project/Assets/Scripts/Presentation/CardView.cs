using Cards.Core;
using TMPro;
using UnityEngine;

namespace Cards.Presentation
{
    /// <summary>
    /// Renders a card's content: thumbnail on the face, title text, and the
    /// 3D model at its anchor. Knows nothing about selection or inspection.
    /// </summary>
    public class CardView : MonoBehaviour, ICardView
    {
        [Header("2D Face")]
        [SerializeField] private Renderer faceRenderer;

        [Header("Title")]
        [SerializeField] private TMP_Text titleText;

        [Header("Back (optional)")]
        [Tooltip("Title repeated on the back of the card.")]
        [SerializeField] private TMP_Text backTitleText;

        [Tooltip("Description text on the back of the card.")]
        [SerializeField] private TMP_Text descriptionText;

        [Header("3D Model")]
        [SerializeField] private Transform modelAnchor;

        [Tooltip("Largest dimension the spawned model may be, in metres. 0 keeps the prefab's own scale.")]
        [SerializeField] private float modelMaxSize = 0.06f;

        [SerializeField] private bool spinModel = true;
        [SerializeField] private float spinSpeed = 30f;

        private GameObject currentModel;

        public void Bind(CardData data)
        {
            if (data == null) return;

            ApplyThumbnail(data);
            ApplyTitle(data);
            ApplyBack(data);
            SpawnModel(data);
        }

        private void ApplyThumbnail(CardData data)
        {
            if (faceRenderer == null || data.Thumbnail == null) return;
            faceRenderer.material.mainTexture = data.Thumbnail.texture;
        }

        private void ApplyTitle(CardData data)
        {
            if (titleText != null) titleText.text = data.Title;
        }

        private void ApplyBack(CardData data)
        {
            if (backTitleText != null) backTitleText.text = data.Title;
            if (descriptionText != null) descriptionText.text = data.Description;
        }

        private void SpawnModel(CardData data)
        {
            if (currentModel != null) Destroy(currentModel);
            if (modelAnchor == null || data.ModelPrefab == null) return;

            // A card prefab spawned inside a card would recurse forever.
            if (data.ModelPrefab.GetComponentInChildren<CardView>() != null)
            {
                Debug.LogError($"{name}: CardData '{data.name}' has a Model Prefab that " +
                               "is itself a card. Ignoring it to avoid infinite recursion.", this);
                return;
            }

            currentModel = Instantiate(data.ModelPrefab, modelAnchor);
            currentModel.transform.localPosition = Vector3.zero;
            currentModel.transform.localRotation = Quaternion.identity;

            foreach (var col in currentModel.GetComponentsInChildren<Collider>())
                col.enabled = false;

            if (modelMaxSize > 0f) FitToSize(currentModel, modelMaxSize);
        }

        private void FitToSize(GameObject model, float maxSize)
        {
            var renderers = model.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return;

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);

            float largest = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
            if (largest <= 0.0001f) return;

            model.transform.localScale *= maxSize / largest;
        }

        private void Update()
        {
            if (spinModel && currentModel != null)
                currentModel.transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.Self);
        }
    }
}
