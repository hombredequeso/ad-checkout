using System;
using System.Collections.Generic;

namespace AdCheckout.Costing.Rules
{
    public class RetailCost<TItem> : IPricingRule<TItem>, IEquatable<RetailCost<TItem>>
    {
        public RetailCost(TItem productCode, decimal cost)
        {
            ProductCode = productCode;
            Cost = cost;
        }

        public TItem ProductCode { get; }
        public decimal Cost { get; }

        public CostingBasket<TItem> ApplyToBasket(CostingBasket<TItem> basket)
        {
            if (basket.ItemCounts.ContainsKey(ProductCode))
            {
                int items = basket.ItemCounts[ProductCode];
                var cost = items * Cost;
                basket.ItemCounts[ProductCode] = 0;
                return new CostingBasket<TItem>(basket.ItemCounts, basket.Cost + cost);
            }

            return basket;
        }
        
        #region ValueEquality
        public bool Equals(RetailCost<TItem> other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return EqualityComparer<TItem>.Default.Equals(ProductCode, other.ProductCode) && Cost == other.Cost;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((RetailCost<TItem>) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (EqualityComparer<TItem>.Default.GetHashCode(ProductCode) * 397) ^ Cost.GetHashCode();
            }
        }

        public static bool operator ==(RetailCost<TItem> left, RetailCost<TItem> right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(RetailCost<TItem> left, RetailCost<TItem> right)
        {
            return !Equals(left, right);
        }
        
        #endregion
    }
}