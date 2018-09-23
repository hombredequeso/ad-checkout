using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using AdCheckout.Costing.Rules;

namespace AdCheckout.Costing
{
    public class Checkout<TItem>
    {
        private readonly List<TItem> _items;
        private readonly List<IPricingRule<TItem>> _pricingRules;

        public Checkout(List<IPricingRule<TItem>> pricingRules)
        {
            _pricingRules = pricingRules;
            _items = new List<TItem>();
        }

        public void Add(TItem item)
        {
            _items.Add(item);
        }

        public decimal Total()
       {
            var groupedItems = _items.GroupBy(x => x).ToImmutableDictionary(i => i.Key, i => i.Count());
            var costingBasket = new CostingBasket<TItem>(groupedItems, 0);
            var result = CostCalculator.ApplyCostings(costingBasket, _pricingRules);
            
            var remainingItems = result.ItemCounts.Where(x => x.Value != 0).ToList();
            if (remainingItems.Any())
            {
                var itemErrorsMsg = string.Join(Environment.NewLine, remainingItems.Select(ToErrorMsg));
                throw new CostingException($"Unable to cost the following items/quantities: {itemErrorsMsg}");
            }

            return result.Cost;
        }
        
        private static string ToErrorMsg<TItem>(KeyValuePair<TItem, int> remainingItems)
        {
            return $"{remainingItems.Key} : {remainingItems.Value}";
        }
    }

    public class CostingException : Exception
    {
        public CostingException(string s)
            :base(s)
        {
        }
    }
}