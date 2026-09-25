using Cards.Adapters;
using Cards.Coordination;
using Cards.Core;
using Cards.Input;
using Cards.Presentation;
using UnityEngine;
using Zenject;

namespace Cards.Installers
{
    /// <summary>
    /// The composition root for the card system. Everything concrete is named
    /// here and nowhere else — this is the one file that knows XRI, the Input
    /// System and the scene hierarchy all exist at the same time.
    ///
    /// Put this on a SceneContext GameObject in the scene.
    /// </summary>
    public class CardSceneInstaller : MonoInstaller
    {
        [Header("Data")]
        [Tooltip("The deck asset the stack and the browser both read from.")]
        [SerializeField] private CardDeck deck;

        [Header("Scene References")]
        [Tooltip("Empty GameObject under the Main Camera marking the inspect pose.")]
        [SerializeField] private InspectAnchorMarker inspectAnchor;

        [Tooltip("Component on the XR rig that reads browse/dismiss actions.")]
        [SerializeField] private InputActionInspectInput inspectInput;

        [Header("Card Prefab")]
        [Tooltip("Prefab with XRCardSelectable + CardView + CardMotion + CardFacade.")]
        [SerializeField] private GameObject cardPrefab;

        [Header("Stack Layout")]
        [SerializeField] private CardStackService.Settings stackSettings;

        public override void InstallBindings()
        {
            // --- Core data -------------------------------------------------
            Container.Bind<ICardProvider>().FromInstance(deck).AsSingle();

            // --- Scene-bound implementations -------------------------------
            Container.Bind<IInspectAnchor>().FromInstance(inspectAnchor).AsSingle();
            Container.Bind<ICardInspectInput>().FromInstance(inspectInput).AsSingle();

            // --- Coordination ----------------------------------------------
            Container.BindInterfacesAndSelfTo<CardInspectionService>().AsSingle();

            Container.BindInstance(stackSettings).AsSingle();
            Container.BindInterfacesAndSelfTo<CardStackService>().AsSingle();

            // --- Per-card factory ------------------------------------------
            // Each spawned card gets its own sub-container, where the three
            // interfaces resolve to that card's own components.
            Container.BindFactory<CardFacade, CardFacade.Factory>()
                .FromComponentInNewPrefab(cardPrefab)
                .UnderTransformGroup("Cards");
        }
    }
}
