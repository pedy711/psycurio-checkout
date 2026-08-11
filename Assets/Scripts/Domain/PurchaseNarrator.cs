using System;
using System.Collections.Generic;

namespace PsyCurio.Shop.Domain
{
    /// <summary>
    /// Turns a basket into the sentence the cashier speaks at the register:
    /// duplicates grouped in first-occurrence order ("2x Coffee"), euro total,
    /// and a distinct sentence for an empty basket so that click never falls silent.
    /// </summary>
    public sealed class PurchaseNarrator
    {
        public const string EmptyBasketSentence = "You haven't picked anything yet.";

        public string Narrate(Basket basket)
        {
            if (basket == null)
            {
                throw new ArgumentNullException(nameof(basket));
            }
            if (basket.Count == 0)
            {
                return EmptyBasketSentence;
            }

            var itemList = JoinWithAnd(GroupedItemPhrases(basket.Items));
            var total = EuroFormatter.Format(basket.TotalCents);
            return $"You chose {itemList}. That makes {total} altogether, please.";
        }

        private static List<string> GroupedItemPhrases(IReadOnlyList<ShopItem> items)
        {
            var order = new List<string>();
            var countsById = new Dictionary<string, int>();
            var namesById = new Dictionary<string, string>();

            foreach (var item in items)
            {
                if (countsById.TryGetValue(item.Id, out var count))
                {
                    countsById[item.Id] = count + 1;
                }
                else
                {
                    order.Add(item.Id);
                    countsById[item.Id] = 1;
                    namesById[item.Id] = item.DisplayName;
                }
            }

            var phrases = new List<string>(order.Count);
            foreach (var id in order)
            {
                phrases.Add($"{countsById[id]}x {namesById[id]}");
            }
            return phrases;
        }

        private static string JoinWithAnd(List<string> phrases)
        {
            if (phrases.Count == 1)
            {
                return phrases[0];
            }

            var allButLast = string.Join(", ", phrases.GetRange(0, phrases.Count - 1));
            return $"{allButLast} and {phrases[phrases.Count - 1]}";
        }
    }
}
