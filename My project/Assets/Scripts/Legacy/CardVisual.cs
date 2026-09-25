using Cards.Core;
using TMPro;
using UnityEngine;

/// <summary>
/// Applies a CardData to a physical card: the 2D thumbnail on its face, the
/// title text, and a small 3D model sitting on/above the card.
///
/// Prefab setup (all optional — leave a field empty to skip that part):
///  - Face Renderer   : the Visual child's Mesh Renderer.
///  - Title Text      : a TextMeshPro - Text (3D) child, sized and placed on the card.
///  - Model Anchor    : an empty child where the 3D model spawns, e.g. just
///                      above the card's face. The model is scaled to fit
///                      Model Max Size so big meshes don't dwarf the card.
/// </summary>
public class CardVisual : MonoBehaviour
{
    [Header("2D Face")]
    [Tooltip("Renderer showing the card's thumbnail. Usually the Visual child's Mesh Renderer.")]
    [SerializeField] private Renderer faceRenderer;

    [Header("Title")]
    [Tooltip("A TextMeshPro - Text (3D) child on the card.")]
    [SerializeField] private TMP_Text titleText;

    [Header("3D Model")]
    [Tooltip("Empty child where the card's 3D model is spawned.")]
    [SerializeField] private Transform modelAnchor;

    [Tooltip("Largest dimension the spawned model is allowed to be, in metres. The model is scaled down to fit. Set to 0 to keep the prefab's own scale.")]
    [SerializeField] private float modelMaxSize = 0.06f;

    [Tooltip("Slowly spin the model so it reads as 3D.")]
    [SerializeField] private bool spinModel = true;
    [SerializeField] private float spinSpeed = 30f;

    private GameObject currentModel;

    public void Bind(CardData data)
    {
        if (data == null) return;

        ApplyThumbnail(data);
        ApplyTitle(data);
        SpawnModel(data);
    }

    private void ApplyThumbnail(CardData data)
    {
        if (faceRenderer == null) return;

        if (data.Thumbnail == null)
        {
            Debug.LogWarning($"{name}: CardData '{data.name}' has no Thumbnail 2D assigned, " +
                             "so the card face keeps its plain material.", this);
            return;
        }

        // Instance the material so each card can show a different image.
        faceRenderer.material.mainTexture = data.Thumbnail.texture;
    }

    private void ApplyTitle(CardData data)
    {
        if (titleText == null) return;
        titleText.text = data.Title;
    }

    private void SpawnModel(CardData data)
    {
        if (currentModel != null)
            Destroy(currentModel);

        if (modelAnchor == null || data.ModelPrefab == null) return;

        currentModel = Instantiate(data.ModelPrefab, modelAnchor);
        currentModel.transform.localPosition = Vector3.zero;
        currentModel.transform.localRotation = Quaternion.identity;

        // Colliders on the model would steal raycasts from the card itself.
        foreach (var col in currentModel.GetComponentsInChildren<Collider>())
            col.enabled = false;

        if (modelMaxSize > 0f)
            FitToSize(currentModel, modelMaxSize);
    }

    /// <summary>
    /// Uniformly scales a spawned model so its largest dimension equals maxSize.
    /// Without this, an imported mesh authored in centimetres (or metres) can
    /// come in wildly larger than the card.
    /// </summary>
    private void FitToSize(GameObject model, float maxSize)
    {
        var renderers = model.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return;

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        float largest = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
        if (largest <= 0.0001f) return;

        float factor = maxSize / largest;
        model.transform.localScale *= factor;
    }

    private void Update()
    {
        if (spinModel && currentModel != null)
            currentModel.transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.Self);
    }
}
