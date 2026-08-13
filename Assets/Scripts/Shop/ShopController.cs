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
        private readonly PurchaseNarrator narrator = new PurchaseNarrator();
        private readonly List<CounterItem> counterViews = new List<CounterItem>();

        public event Action PlacementAccepted;
        public event Action PlacementRefused;
        public event Action ItemRemoved;
        public event Action ShopReset;

        public Basket Basket => basket;

        public void TryPlace(ShopItemDefinition definition, Vector3 fromWorldPosition)
        {
            // Refuse before touching the domain: a basket entry without a
            // matching view would desync removal-by-index for the session.
            if (counterSlots == null || counterSlots.Count < Basket.Capacity)
            {
                Debug.LogError("ShopController: counter slots missing or fewer than "
                    + $"Basket.Capacity ({Basket.Capacity}) — placement disabled.", this);
                return;
            }
            if (definition == null || definition.CounterPrefab == null)
            {
                var definitionName = definition == null ? "<null>" : definition.name;
                Debug.LogError($"ShopController: item definition '{definitionName}' has no counter prefab — cannot place.", this);
                return;
            }

            var result = basket.Add(definition.ToDomainItem());
            if (result.WasAccepted)
            {
                var spawned = counterSlots.Place(result.SlotIndex, definition, fromWorldPosition);
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
                SayThroughCashier(RefusalLine);
                PlacementRefused?.Invoke();
            }
        }

        /// <summary>Register click: the cashier states the chosen items and
        /// the total. Lives here so domain access stays behind this bridge.</summary>
        public void NarratePurchase()
        {
            SayThroughCashier(narrator.Narrate(basket));
        }

        public void Remove(CounterItem view)
        {
            var index = counterViews.IndexOf(view);
            if (index < 0)
            {
                Debug.LogWarning($"ShopController: '{view.name}' is not a tracked counter item — ignoring.", view);
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
            if (cashier != null)
            {
                cashier.Silence();
            }
            ShopReset?.Invoke();
        }

        private void SayThroughCashier(string line)
        {
            if (cashier != null)
            {
                cashier.Say(line);
            }
            else
            {
                Debug.LogError($"ShopController: cashier not wired — cannot say '{line}'.", this);
            }
        }
    }
}
