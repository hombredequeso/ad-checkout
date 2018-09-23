using System;

namespace AdCheckout.Costing.Rules
{
    public interface IPricingRule<TItem>
    {
        CostingBasket<TItem> ApplyToBasket(CostingBasket<TItem> basket);
    }
}