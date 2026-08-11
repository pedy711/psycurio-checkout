using System;
using PsyCurio.Shop.Domain;
using UnityEngine;

namespace PsyCurio.Shop
{
    /// <summary>
    /// The single bridge between clicks and the domain. Owns the one Basket
    /// instance and translates its explicit results into scene changes; it
    /// holds no shop rules itself — the domain assembly decides, this class
    /// renders the decision.
    /// </summary>
    public sealed class ShopController : MonoBehaviour
    {
        [SerializeField] private CounterSlots counterSlots;

        private readonly Basket basket = new Basket();

        /// <summary>Raised when the basket refuses a sixth item; the usability
        /// pass attaches the slot pulse and the cashier's spoken line here.</summary>
        public event Action PlacementRefused;

        public Basket Basket => basket;

        public void TryPlace(ShopItemDefinition definition)
        {
            var result = basket.Add(definition.ToDomainItem());
            if (result.WasAccepted)
            {
                counterSlots.Place(result.SlotIndex, definition);
            }
            else
            {
                PlacementRefused?.Invoke();
            }
        }

        public void RemoveAt(int basketIndex)
        {
            basket.RemoveAt(basketIndex);
            counterSlots.ShiftDownFrom(basketIndex);
        }

        public void ResetShop()
        {
            basket.Clear();
            counterSlots.Clear();
        }
    }
}
