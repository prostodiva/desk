using Cards.Core;
using UnityEngine;
using Zenject;

namespace Cards.Installers
{
    /// <summary>
    /// Binds one card's own components to the Core interfaces, so CardFacade
    /// receives that card's selectable/view/motion rather than some other
    /// card's.
    ///
    /// Put this on the card prefab's root, alongside a GameObjectContext.
    /// </summary>
    public class CardPrefabInstaller : MonoInstaller
    {
        [Tooltip("The XRCardSelectable on this card.")]
        [SerializeField] private MonoBehaviour selectable;

        [Tooltip("The CardView on this card.")]
        [SerializeField] private MonoBehaviour view;

        [Tooltip("The CardMotion on this card.")]
        [SerializeField] private MonoBehaviour motion;

        public override void InstallBindings()
        {
            Container.Bind<ICardSelectable>().FromInstance((ICardSelectable)selectable).AsSingle();
            Container.Bind<ICardView>().FromInstance((ICardView)view).AsSingle();
            Container.Bind<ICardMotion>().FromInstance((ICardMotion)motion).AsSingle();
        }
    }
}
