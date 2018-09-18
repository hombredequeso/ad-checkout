using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Xunit;

namespace AdCheckoutTests
{
    public class CheckoutTests
    {
        [Fact]
        public void No_Items_Totals_Zero()
        {
            new Checkout<string>(GetPricingDictionary())
                .Total()
                .Should().Be(0);
        }

        [Fact]
        public void One_Item_No_Rules_Equals_Item_Price()
        {
            const string item = "item1";
            const decimal price = 269.99m;
            var itemPricing = new Dictionary<string, decimal>
            {
                {"item1", price}
            };
            
            var checkout = new Checkout<string>(itemPricing);
            checkout.Add(item);
            
            decimal total = checkout.Total();
            total.Should().Be(price);
        }

        private static string RandomProductId()
        {
            return Guid.NewGuid().ToString();
        }

        [Fact]
        public void One_Of_Every_Item_Should_Cost_Total_Of_All_Items()
        {
            var itemPricing = 
                new List<decimal>{1.2m, 3.4m, 9.3m}
                .ToDictionary(_ => RandomProductId(), p => p);
            
            var checkout = new Checkout<string>(itemPricing);
            foreach (var pricing in itemPricing)
            {
                checkout.Add(pricing.Key);
                
            }
            checkout.Total().Should().Be(itemPricing.Values.Sum(p => p));
        }

        [Theory]
        [InlineData(0,0)]    
        [InlineData(1, 1)]
        [InlineData(2, 2)]
        [InlineData(3, 2)]
        [InlineData(4, 3)]
        [InlineData(5, 4)]
        [InlineData(6, 4)]
        [InlineData(7, 5)]
        public void ThreeForTwo_Discount_Tests(int checkoutItemCount, int payingForItemCount)
        {
            var discountAmount = 3;
            var standardPricing = GetPricingDictionary();
            var discountedItem = standardPricing.First();
            var volumeDiscount = new VolumeDiscount<string>(discountedItem.Key, discountAmount, discountAmount-1);

            var checkout = new Checkout<string>(standardPricing, volumeDiscount);
            for (var c =0; c < checkoutItemCount; c++)
                checkout.Add(discountedItem.Key);

            checkout.Total().Should().Be(standardPricing.First().Value * payingForItemCount);
        }
        
        [Fact]
        public void Two_Of_Every_Item_Should_Equal_Twice_Total_Of_All_Pricings()
        {
            var pricingDictionary = GetPricingDictionary();
            
            var checkout = new Checkout<string>(pricingDictionary);
            foreach (var pricing in pricingDictionary)
            {
                checkout.Add(pricing.Key);
                checkout.Add(pricing.Key);
            }
            checkout.Total().Should().Be(2 *pricingDictionary.Values.Sum(p => p));
        }

        [Fact]
        public void VolumeDiscount_Is_Not_Applied_To_Items_Not_Volume_Discounted()
        {
            var standardPricing = GetPricingDictionary();

            var item = standardPricing.First();
            const int discountAmount = 3;
            const int gotAmount = discountAmount - 1;
            var volumeDiscount = 
                new VolumeDiscount<string>(item.Key, discountAmount, discountAmount - 1);

            var checkout = new Checkout<string>(standardPricing, volumeDiscount);
            for (int c =0; c < gotAmount; c++)
                checkout.Add(item.Key);

            checkout.Total().Should().Be(standardPricing.First().Value * gotAmount);
        }

        private static Dictionary<string, decimal> GetPricingDictionary()
        {
            return 
                new List<decimal>{1.2m, 3.4m, 9.3m}
                .ToDictionary(
                        _ => RandomProductId(),
                        p => p);
        }
    }

    public class VolumeDiscount<TItem>
    {
        public VolumeDiscount(
            TItem item, 
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
            Get = get;
            ForPriceOf = forPriceOf;
        }

        public TItem Item { get; }
        public int Get { get; }
        public int ForPriceOf { get; }
    }

    public class Checkout<TItem>
    {
        private readonly List<TItem> _items;
        private readonly IDictionary<TItem, decimal> _stdPricing;
        private readonly VolumeDiscount<TItem>[] _pricingRules;

        public Checkout(
            IDictionary<TItem, decimal> stdPricing,
            params VolumeDiscount<TItem>[] pricingRules)
        {
            _items = new List<TItem>();
            _stdPricing = stdPricing;
            _pricingRules = pricingRules;
            _pricingRules = pricingRules;
        }

        public decimal Total()
        {
            var costingItems = _items
                    .GroupBy(i => i)
                    .ToDictionary(
                        x => x.Key, 
                        x => (_stdPricing[x.Key], x.Count()));
            
            var calculation = new CostingCalculation<TItem>(
                costingItems,
                _pricingRules.ToList(),
                0);

            return CostCalculator.GetCost(calculation).Cost;
        }
        
        public void Add(TItem item)
        {
            _items.Add(item);
        }
    }
    
    
    public class CostingCalculation<TItem>
    {
        public CostingCalculation(
            Dictionary<TItem, (decimal, int)> items, 
            List<VolumeDiscount<TItem>> discounts, 
            decimal cost)
        {
            Items = items ?? throw new ArgumentNullException(nameof(items));
            Discounts = discounts ?? throw new ArgumentNullException(nameof(discounts));
            Cost = cost;
        }

        public Dictionary<TItem, (decimal, int)> Items { get; }
        public List<VolumeDiscount<TItem>> Discounts { get; }
        public decimal Cost { get; }
    }

    public static class CostCalculator
    {
        public static CostingCalculation<TItem> GetCost<TItem>(
            CostingCalculation<TItem> costingCalculation)
        {
            return costingCalculation.Discounts.Any()
                ? GetCost(ApplyNextDiscount(costingCalculation))
                : BaseCostAllItems(costingCalculation);
        }

        private static CostingCalculation<TItem> ApplyNextDiscount<TItem>(
            CostingCalculation<TItem> costingCalculation)
        {
            var (discount, remainingDiscounts) = 
                HeadAndTail(costingCalculation.Discounts);

            if (costingCalculation.Items.ContainsKey(discount.Item))
            {
                var (itemCost, itemQuantity) = costingCalculation.Items[discount.Item];
                var (quotient, remainder) = DivMod(itemQuantity, discount.Get);
                
                costingCalculation.Items[discount.Item] = (itemCost, remainder);
                var cost = quotient * discount.ForPriceOf * itemCost;

                return new CostingCalculation<TItem>(
                    costingCalculation.Items, 
                    remainingDiscounts, 
                    costingCalculation.Cost + cost);
            }
            
            return new CostingCalculation<TItem>(
                costingCalculation.Items, 
                remainingDiscounts, 
                costingCalculation.Cost);
        }

        private static CostingCalculation<TItem> BaseCostAllItems<TItem>(CostingCalculation<TItem> c)
        {
            var allItems = c.Items;
            var noItems = new Dictionary<TItem, (decimal, int)>();

            return new CostingCalculation<TItem>(
                noItems,
                c.Discounts, 
                c.Cost + allItems.Sum(i => i.Value.Item1 * i.Value.Item2));
        }
        
        private static (int, int) DivMod(int dividend, int divisor)
        {
            return (dividend / divisor, dividend % divisor);
        }

        private static (T, List<T>) HeadAndTail<T>(List<T> l)
        {
            return (l.First(), l.Skip(1).ToList());

        }
    }
}
