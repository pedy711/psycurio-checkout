using System;
using System.Collections.Generic;
using PsyCurio.Shop.Domain;
using UnityEngine;

namespace PsyCurio.Shop
{
    /// <summary>
    /// The single bridge between clicks and the domain. Owns the one Basket
    /// instance and translates its explicit results into scene changes; it
    /// holds no shop rules itself — the domain assembly decides, this class
    /// renders the decision. Also keeps the spawned counter views aligned
    /// with basket indices so removal-by-click maps cleanly onto RemoveAt.
    /// </summary>
    public sealed class ShopController : MonoBehaviour
    {
        public const string RefusalLine = "That's all I can carry at once.";

        [SerializeField] private CounterSlots counterSlots;
        [SerializeField] private Cashier cashier;

        private readonly Basket basket = new Basket();
        private readonly List<CounterItem> counterViews = new List<CounterItem>();

        public event Action PlacementAccepted;
        public event Action PlacementRefused;
        public event Action ItemRemoved;
        public event Action ShopReset;

        public Basket Basket => basket;

        public void TryPlace(ShopItemDefinition definition)
        {
            var result = basket.Add(definition.ToDomainItem());
            if (result.WasAccepted)
            {
                var spawned = counterSlots.Place(result.SlotIndex, definition);
                var view = spawned.AddComponent<CounterItem>();
                view.Init(this);
                counterViews.Add(view);
                PlacementAccepted?.Invoke();
            }
            else
            {
                // A refused sixth item must be unmissable: markers pulse, the
                // cashier says so, and ClickFeedback adds the refusal sound.
                counterSlots.PulseMarkers();
                cashier.Say(RefusalLine);
                PlacementRefused?.Invoke();
            }
        }

        public void Remove(CounterItem view)
        {
            var index = counterViews.IndexOf(view);
            if (index < 0)
            {
                return;
            }

            basket.RemoveAt(index);
            counterViews.RemoveAt(index);
            counterSlots.ShiftDownFrom(index);
            ItemRemoved?.Invoke();
        }

        public void ResetShop()
        {
            basket.Clear();
            counterViews.Clear();
            counterSlots.Clear();
            ShopReset?.Invoke();
        }
    }
}
