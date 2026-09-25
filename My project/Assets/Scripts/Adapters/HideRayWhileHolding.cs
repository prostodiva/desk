using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace Cards.Adapters
{
    /// <summary>
    /// Hides an interactor's ray while it holds something, so the laser
    /// doesn't draw across the object being read.
    /// </summary>
    [RequireComponent(typeof(XRBaseInteractor))]
    public class HideRayWhileHolding : MonoBehaviour
    {
        [SerializeField] private GameObject lineVisualObject;

        private XRBaseInteractor interactor;

        private void Awake()
        {
            interactor = GetComponent<XRBaseInteractor>();

            if (lineVisualObject == null)
            {
                Transform found = transform.Find("LineVisual");
                if (found != null) lineVisualObject = found.gameObject;
            }
        }

        private void OnEnable()
        {
            interactor.selectEntered.AddListener(OnSelectEntered);
            interactor.selectExited.AddListener(OnSelectExited);
        }

        private void OnDisable()
        {
            interactor.selectEntered.RemoveListener(OnSelectEntered);
            interactor.selectExited.RemoveListener(OnSelectExited);
        }

        private void OnSelectEntered(SelectEnterEventArgs args) => SetVisible(false);
        private void OnSelectExited(SelectExitEventArgs args) => SetVisible(true);

        private void SetVisible(bool visible)
        {
            if (lineVisualObject != null) lineVisualObject.SetActive(visible);
        }
    }
}
