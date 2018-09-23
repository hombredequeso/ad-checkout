using System.Collections.Generic;

namespace AdCheckout.Costing
{
    public class CostingBasket<TItem>
    {
        public CostingBasket(
            Dictionary<TItem, int> itemCounts,
            decimal cost)
        {
            ItemCounts = itemCounts;
            Cost = cost;
        }

        public Dictionary<TItem, int> ItemCounts { get; }
        public decimal Cost { get; }
    }
}