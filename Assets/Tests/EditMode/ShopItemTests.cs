using System;
using NUnit.Framework;
using PsyCurio.Shop.Domain;

namespace PsyCurio.Shop.Domain.Tests
{
    /// <summary>
    /// The constructor guards are the domain's boundary against misconfigured
    /// inspector content: a bad ShopItemDefinition must fail at construction
    /// time, not at narration time in front of a patient.
    /// </summary>
    public sealed class ShopItemTests
    {
        [Test]
        public void Constructor_ValidInput_ExposesAllValues()
        {
            var item = new ShopItem("coffee", "Coffee", 249);

            Assert.That(item.Id, Is.EqualTo("coffee"));
            Assert.That(item.DisplayName, Is.EqualTo("Coffee"));
            Assert.That(item.PriceCents, Is.EqualTo(249));
        }

        [Test]
        public void Constructor_ZeroPrice_IsAllowed()
        {
            Assert.That(new ShopItem("sample", "Free Sample", 0).PriceCents, Is.EqualTo(0));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void Constructor_MissingId_Throws(string badId)
        {
            Assert.Throws<ArgumentException>(() => new ShopItem(badId, "Coffee", 249));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void Constructor_MissingDisplayName_Throws(string badName)
        {
            Assert.Throws<ArgumentException>(() => new ShopItem("coffee", badName, 249));
        }

        [Test]
        public void Constructor_NegativePrice_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new ShopItem("coffee", "Coffee", -1));
        }
    }
}
