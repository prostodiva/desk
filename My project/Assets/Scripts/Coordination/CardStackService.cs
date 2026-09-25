using System.Collections.Generic;
using Cards.Core;
using UnityEngine;
using Zenject;

namespace Cards.Coordination
{
    /// <summary>
    /// Spawns one card per deck entry and arranges them on the table. Cards
    /// come from an injected factory, so this service never calls Instantiate
    /// or knows which prefab is used.
    /// </summary>
    public class CardStackService : IInitializable
    {
        public enum LayoutMode { DrawPile, Fan }

        private readonly ICardProvider provider;
        private readonly CardFacade.Factory cardFactory;
        private readonly Settings settings;

        private readonly List<CardFacade> cards = new List<CardFacade>();

        public CardStackService(
            ICardProvider provider,
            CardFacade.Factory cardFactory,
            Settings settings)
        {
            this.provider = provider;
            this.cardFactory = cardFactory;
            this.settings = settings;
        }

        public void Initialize() => Build();

        /// <summary>
        /// Rebuilds the stack from scratch: every card back in its slot with
        /// its own content. Doubles as the "reset everything" entry point.
        /// </summary>
        public void Build()
        {
            Clear();

            foreach (CardData data in provider.Cards)
            {
                CardFacade card = cardFactory.Create();
                card.transform.SetParent(settings.Root, worldPositionStays: false);
                card.SetData(data);
                cards.Add(card);
            }

            Arrange();
        }

        private void Clear()
        {
            foreach (var card in cards)
                if (card != null) Object.Destroy(card.gameObject);
            cards.Clear();
        }

        private void Arrange()
        {
            if (cards.Count == 0) return;

            if (settings.Layout == LayoutMode.DrawPile)
                ArrangeAsDrawPile();
            else
                ArrangeAsFan();

            foreach (var card in cards)
                card.Motion.CaptureHome();
        }

        private void ArrangeAsDrawPile()
        {
            for (int i = 0; i < cards.Count; i++)
            {
                cards[i].transform.localPosition = new Vector3(0f, settings.StackSpacing * i, 0f);
                cards[i].transform.localRotation = Quaternion.identity;

                // Only the top card is selectable; the rest stay inert.
                cards[i].SetInteractable(i == cards.Count - 1);
            }
        }

        private void ArrangeAsFan()
        {
            int count = cards.Count;
            for (int i = 0; i < count; i++)
            {
                float t = count > 1 ? (float)i / (count - 1) : 0.5f;
                float angle = Mathf.Lerp(-settings.FanArcAngle * 0.5f, settings.FanArcAngle * 0.5f, t);

                Vector3 localPos;
                Quaternion localRot;

                if (settings.UseArc)
                {
                    float radius = settings.FanSpacing * count * 0.5f;
                    Vector3 baseOffset = new Vector3(0f, radius, 0f);
                    localPos = Quaternion.Euler(0f, 0f, angle) * baseOffset - baseOffset;
                    localRot = Quaternion.Euler(0f, 0f, angle);
                }
                else
                {
                    float x = (i - (count - 1) * 0.5f) * settings.FanSpacing;
                    localPos = new Vector3(x, 0f, 0f);
                    localRot = Quaternion.identity;
                }

                // Tiny stagger so coplanar colliders don't fight over the raycast.
                localPos += new Vector3(0f, 0.0005f * i, 0f);

                cards[i].transform.localPosition = localPos;
                cards[i].transform.localRotation = localRot;
                cards[i].SetInteractable(true);
            }
        }

        [System.Serializable]
        public class Settings
        {
            public Transform Root;
            public LayoutMode Layout = LayoutMode.Fan;
            public float StackSpacing = 0.003f;
            public float FanSpacing = 0.05f;
            public float FanArcAngle = 30f;
            public bool UseArc = true;
        }
    }
}
