using System;
using NUnit.Framework;
using PsyCurio.Shop.Domain;

namespace PsyCurio.Shop.Domain.Tests
{
    public sealed class EuroFormatterTests
    {
        [TestCase(0, "0,00 €")]
        [TestCase(5, "0,05 €")]
        [TestCase(99, "0,99 €")]
        [TestCase(100, "1,00 €")]
        [TestCase(305, "3,05 €")]
        [TestCase(747, "7,47 €")]
        [TestCase(12450, "124,50 €")]
        public void Format_RendersGermanStyleEuros(int cents, string expected)
        {
            Assert.That(EuroFormatter.Format(cents), Is.EqualTo(expected));
        }

        [Test]
        public void Format_NegativeAmount_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => EuroFormatter.Format(-1));
        }
    }
}
