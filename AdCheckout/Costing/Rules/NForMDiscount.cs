using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace AdCheckout.Costing.Rules
{
    public class NForMDiscount<TItem> : IPricingRule<TItem>, IEquatable<NForMDiscount<TItem>>
    {

        public NForMDiscount(
            TItem item,
            decimal itemCost,
            int get,
            int forPriceOf)
        {
            if (get == 0)
                throw new ArgumentException("Cannot get nothing for something :-)", nameof(get));
            if (forPriceOf < 0)
                throw new ArgumentException("cannot be less than 0", nameof(forPriceOf));
            if (item == null)
                throw new ArgumentException("Cannot have null item", nameof(item));
            Item = item;
            ItemCost = itemCost;
            Get = get;
            ForPriceOf = forPriceOf;
        }

        public TItem Item { get; }
        public decimal ItemCost { get; }
        public int Get { get; }
        public int ForPriceOf { get; }

        public CostingBasket<TItem> ApplyToBasket(CostingBasket<TItem> basket)
        {
            if (basket.ItemCounts.ContainsKey(Item))
            {
                int itemCount = basket.ItemCounts[Item];
                var (quotient, remainder) = DivMod(itemCount, Get);

                var itemsOut = basket.ItemCounts.SetItem(Item, remainder);
                var cost = quotient * ForPriceOf * ItemCost;
                return new CostingBasket<TItem>(itemsOut, basket.Cost + cost);
            }

            return basket;
        }


        private static (int, int) DivMod(int dividend, int divisor)
        {
            return (dividend / divisor, dividend % divisor);
        }
        
        #region ValueEquality 
        public bool Equals(NForMDiscount<TItem> other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return EqualityComparer<TItem>.Default.Equals(Item, other.Item) && ItemCost == other.ItemCost && Get == other.Get && ForPriceOf == other.ForPriceOf;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((NForMDiscount<TItem>) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = EqualityComparer<TItem>.Default.GetHashCode(Item);
                hashCode = (hashCode * 397) ^ ItemCost.GetHashCode();
                hashCode = (hashCode * 397) ^ Get;
                hashCode = (hashCode * 397) ^ ForPriceOf;
                return hashCode;
            }
        }

        public static bool operator ==(NForMDiscount<TItem> left, NForMDiscount<TItem> right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(NForMDiscount<TItem> left, NForMDiscount<TItem> right)
        {
            return !Equals(left, right);
        }
        
        #endregion
    }
}