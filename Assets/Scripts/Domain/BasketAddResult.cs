namespace PsyCurio.Shop.Domain
{
    /// <summary>
    /// Explicit outcome of <see cref="Basket.Add"/>. A refused add is a normal,
    /// expected result the caller must handle visibly — never a silent no-op.
    /// </summary>
    public readonly struct BasketAddResult
    {
        public bool WasAccepted { get; }

        /// <summary>Slot the item was placed in; only meaningful when <see cref="WasAccepted"/>.</summary>
        public int SlotIndex { get; }

        private BasketAddResult(bool wasAccepted, int slotIndex)
        {
            WasAccepted = wasAccepted;
            SlotIndex = slotIndex;
        }

        public static BasketAddResult AcceptedAt(int slotIndex)
        {
            return new BasketAddResult(true, slotIndex);
        }

        public static BasketAddResult RejectedFull()
        {
            return new BasketAddResult(false, -1);
        }
    }
}
