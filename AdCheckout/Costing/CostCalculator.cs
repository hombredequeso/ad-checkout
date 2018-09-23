using System;
using System.Collections.Generic;
using System.Linq;
using AdCheckout.Costing.Rules;

namespace AdCheckout.Costing
{
    public static class CostCalculator
    {
        public static CostingBasket<TItem> ApplyCostings<TItem>(
            CostingBasket<TItem> basketIn,
            List<IPricingRule<TItem>> rules)
        {
            CostingBasket<TItem> result = rules.Aggregate(
                basketIn,
                (basket, rule) => rule.ApplyToBasket(basket));
            return result;
        }
    }
}