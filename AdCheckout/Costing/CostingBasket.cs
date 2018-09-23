using System.Collections.Immutable;

namespace AdCheckout.Costing
{
    public class CostingBasket<TItem>
    {
        public CostingBasket(
            ImmutableDictionary<TItem, int> itemCounts,
            decimal cost)
        {
            ItemCounts = itemCounts;
            Cost = cost;
        }

        public ImmutableDictionary<TItem, int> ItemCounts { get; }
        public decimal Cost { get; }
    }
}