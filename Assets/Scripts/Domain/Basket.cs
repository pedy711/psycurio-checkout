using System;
using System.Collections.Generic;

namespace PsyCurio.Shop.Domain
{
    /// <summary>
    /// The items currently on the counter, in placement order. Capacity-limited;
    /// duplicates allowed. Owns the shopping rules — the scene layer only renders
    /// what this class decides.
    /// </summary>
    public sealed class Basket
    {
        public const int Capacity = 5;

        private readonly List<ShopItem> items = new List<ShopItem>();

        public IReadOnlyList<ShopItem> Items => items;

        public int Count => items.Count;

        public bool IsFull => items.Count >= Capacity;

        public int TotalCents
        {
            get
            {
                var total = 0;
                foreach (var item in items)
                {
                    total += item.PriceCents;
                }
                return total;
            }
        }

        public BasketAddResult Add(ShopItem item)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }
            if (IsFull)
            {
                return BasketAddResult.RejectedFull();
            }

            items.Add(item);
            return BasketAddResult.AcceptedAt(items.Count - 1);
        }

        /// <summary>Removes the item at <paramref name="index"/>; later items shift down one slot.</summary>
        public void RemoveAt(int index)
        {
            if (index < 0 || index >= items.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index), index, $"No item at index {index}; count is {items.Count}.");
            }

            items.RemoveAt(index);
        }

        public void Clear()
        {
            items.Clear();
        }
    }
}
