using System;

namespace PsyCurio.Shop.Domain
{
    /// <summary>
    /// An item the shop sells. Immutable. Money is integer cents throughout the
    /// domain; euros exist only at the inspector boundary.
    /// </summary>
    public sealed class ShopItem
    {
        public string Id { get; }
        public string DisplayName { get; }
        public int PriceCents { get; }

        public ShopItem(string id, string displayName, int priceCents)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("Id must be non-empty.", nameof(id));
            }
            if (string.IsNullOrWhiteSpace(displayName))
            {
                throw new ArgumentException("DisplayName must be non-empty.", nameof(displayName));
            }
            if (priceCents < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(priceCents), priceCents, "Price must not be negative.");
            }

            Id = id;
            DisplayName = displayName;
            PriceCents = priceCents;
        }
    }
}
