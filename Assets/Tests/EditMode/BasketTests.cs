using System;
using NUnit.Framework;
using PsyCurio.Shop.Domain;

namespace PsyCurio.Shop.Domain.Tests
{
    public sealed class BasketTests
    {
        private static ShopItem Item(string id = "coffee", string name = "Coffee", int cents = 249)
        {
            return new ShopItem(id, name, cents);
        }

        [Test]
        public void NewBasket_IsEmpty()
        {
            var basket = new Basket();

            Assert.That(basket.Count, Is.EqualTo(0));
            Assert.That(basket.IsFull, Is.False);
            Assert.That(basket.TotalCents, Is.EqualTo(0));
        }

        [Test]
        public void Add_WithinCapacity_AcceptsWithSequentialSlotIndices()
        {
            var basket = new Basket();

            for (var i = 0; i < Basket.Capacity; i++)
            {
                var result = basket.Add(Item());

                Assert.That(result.WasAccepted, Is.True, $"add #{i + 1} should be accepted");
                Assert.That(result.SlotIndex, Is.EqualTo(i), $"add #{i + 1} should land in slot {i}");
            }

            Assert.That(basket.Count, Is.EqualTo(Basket.Capacity));
            Assert.That(basket.IsFull, Is.True);
        }

        [Test]
        public void Add_SixthItem_IsRejectedAndBasketUnchanged()
        {
            var basket = new Basket();
            for (var i = 0; i < Basket.Capacity; i++)
            {
                basket.Add(Item());
            }

            var result = basket.Add(Item("bread", "Bread", 109));

            Assert.That(result.WasAccepted, Is.False);
            Assert.That(basket.Count, Is.EqualTo(Basket.Capacity));
            Assert.That(basket.TotalCents, Is.EqualTo(Basket.Capacity * 249));
        }

        [Test]
        public void Add_Null_Throws()
        {
            var basket = new Basket();

            Assert.Throws<ArgumentNullException>(() => basket.Add(null));
        }

        [Test]
        public void Add_AllowsDuplicatesOfTheSameItem()
        {
            var basket = new Basket();
            var coffee = Item();

            var first = basket.Add(coffee);
            var second = basket.Add(coffee);

            Assert.That(first.WasAccepted, Is.True);
            Assert.That(second.WasAccepted, Is.True);
            Assert.That(basket.Count, Is.EqualTo(2));
        }

        [Test]
        public void RemoveAt_RemovesItemAndShiftsLaterItemsDown()
        {
            var basket = new Basket();
            basket.Add(Item("coffee", "Coffee", 249));
            basket.Add(Item("bread", "Bread", 109));
            basket.Add(Item("milk", "Milk", 89));

            basket.RemoveAt(1);

            Assert.That(basket.Count, Is.EqualTo(2));
            Assert.That(basket.Items[0].Id, Is.EqualTo("coffee"));
            Assert.That(basket.Items[1].Id, Is.EqualTo("milk"));
        }

        [Test]
        public void RemoveAt_OutOfRange_Throws()
        {
            var basket = new Basket();
            basket.Add(Item());

            Assert.Throws<ArgumentOutOfRangeException>(() => basket.RemoveAt(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => basket.RemoveAt(1));
        }

        [Test]
        public void RemoveAt_FreedSlot_CanBeFilledAgain()
        {
            var basket = new Basket();
            for (var i = 0; i < Basket.Capacity; i++)
            {
                basket.Add(Item());
            }
            basket.RemoveAt(2);

            var result = basket.Add(Item("bread", "Bread", 109));

            Assert.That(result.WasAccepted, Is.True);
            Assert.That(result.SlotIndex, Is.EqualTo(Basket.Capacity - 1));
        }

        [Test]
        public void Clear_EmptiesTheBasket()
        {
            var basket = new Basket();
            basket.Add(Item());
            basket.Add(Item());

            basket.Clear();

            Assert.That(basket.Count, Is.EqualTo(0));
            Assert.That(basket.TotalCents, Is.EqualTo(0));
        }

        [Test]
        public void TotalCents_SumsAllItemsIncludingDuplicates()
        {
            var basket = new Basket();
            basket.Add(Item("coffee", "Coffee", 249));
            basket.Add(Item("coffee", "Coffee", 249));
            basket.Add(Item("bread", "Bread", 109));

            Assert.That(basket.TotalCents, Is.EqualTo(249 + 249 + 109));
        }
    }
}
