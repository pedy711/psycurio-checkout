using System;
using NUnit.Framework;
using PsyCurio.Shop.Domain;

namespace PsyCurio.Shop.Domain.Tests
{
    public sealed class PurchaseNarratorTests
    {
        private readonly PurchaseNarrator narrator = new PurchaseNarrator();

        private static Basket BasketWith(params ShopItem[] items)
        {
            var basket = new Basket();
            foreach (var item in items)
            {
                Assert.That(basket.Add(item).WasAccepted, Is.True,
                    "test setup must not overflow the basket silently");
            }
            return basket;
        }

        [Test]
        public void Narrate_EmptyBasket_UsesDistinctSentence()
        {
            var sentence = narrator.Narrate(new Basket());

            Assert.That(sentence, Is.EqualTo(PurchaseNarrator.EmptyBasketSentence));
        }

        [Test]
        public void Narrate_Null_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => narrator.Narrate(null));
        }

        [Test]
        public void Narrate_SingleItem_NamesItAndItsPrice()
        {
            var basket = BasketWith(new ShopItem("bread", "Bread", 109));

            var sentence = narrator.Narrate(basket);

            Assert.That(sentence, Is.EqualTo("You chose 1x Bread. That makes 1,09 € altogether, please."));
        }

        [Test]
        public void Narrate_GroupsDuplicatesWithCount()
        {
            var coffee = new ShopItem("coffee", "Coffee", 249);
            var basket = BasketWith(coffee, coffee);

            var sentence = narrator.Narrate(basket);

            Assert.That(sentence, Is.EqualTo("You chose 2x Coffee. That makes 4,98 € altogether, please."));
        }

        [Test]
        public void Narrate_GroupsNonAdjacentDuplicates_InFirstOccurrenceOrder()
        {
            var coffee = new ShopItem("coffee", "Coffee", 249);
            var bread = new ShopItem("bread", "Bread", 109);
            var basket = BasketWith(coffee, bread, coffee);

            var sentence = narrator.Narrate(basket);

            Assert.That(sentence, Is.EqualTo("You chose 2x Coffee and 1x Bread. That makes 6,07 € altogether, please."));
        }

        [Test]
        public void Narrate_ThreeGroups_JoinsWithCommasAndFinalAnd()
        {
            var basket = BasketWith(
                new ShopItem("coffee", "Coffee", 249),
                new ShopItem("coffee", "Coffee", 249),
                new ShopItem("bread", "Bread", 109),
                new ShopItem("milk", "Milk", 89));

            var sentence = narrator.Narrate(basket);

            Assert.That(sentence, Is.EqualTo(
                "You chose 2x Coffee, 1x Bread and 1x Milk. That makes 6,96 € altogether, please."));
        }

        [Test]
        public void Narrate_FullBasketOfOneItem_CountsAllFive()
        {
            var apple = new ShopItem("apples", "Apples", 199);
            var basket = BasketWith(apple, apple, apple, apple, apple);

            var sentence = narrator.Narrate(basket);

            Assert.That(sentence, Is.EqualTo("You chose 5x Apples. That makes 9,95 € altogether, please."));
        }
    }
}
