using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using FluentAssertions;
using Xunit;

namespace AdCheckoutTests
{
    public class CheckoutTests
    {
        [Fact]
        public void No_Items_Totals_Zero()
        {
            Checkout co = new Checkout(GetPricingDictionary());
            decimal total = co.Total();
            total.Should().Be(0);
        }

        [Fact]
        public void One_Item_No_Rules_Equals_Item_Price()
        {
            string item = "item1";
            decimal price = 269.99m;
            Dictionary<string, decimal> itemPricing = new Dictionary<string, decimal>()
            {
                {"item1", price}
            };
            
            Checkout co = new Checkout(itemPricing);
            co.Add(item);
            
            decimal total = co.Total();
            total.Should().Be(price);
        }

        private static string RandomProductId()
        {
            return Guid.NewGuid().ToString();
        }

        [Fact]
        public void One_Of_Every_Item_Should_Equal_Total_Of_All_Pricings()
        {
            var pricingDictionary = 
                new List<decimal>{1.2m, 3.4m, 9.3m}
                .ToDictionary(_ => RandomProductId(), p => p);
            
            Checkout co = new Checkout(pricingDictionary);
            foreach (var pricing in pricingDictionary)
            {
                co.Add(pricing.Key);
                
            }
            co.Total().Should().Be(pricingDictionary.Values.Sum(p => p));
        }

        [Theory]
        [InlineData(0,0)]    // practically non-sensical, but mathematically theoretically correct.
        [InlineData(1, 1)]
        [InlineData(2, 2)]
        [InlineData(3, 2)]
        [InlineData(4, 3)]
        [InlineData(5, 4)]
        [InlineData(6, 4)]
        [InlineData(7, 5)]
        public void ThreeForTwo_Discount_Tests(int gotAmount, int payFor)
        {
            int discountAmount = 3;
            Dictionary<string, decimal> standardPricing = GetPricingDictionary();
            var discountedItem = standardPricing.First();
            var volumeDiscount = new VolumeDiscount<string>(discountedItem.Key, discountAmount, discountAmount-1);

            Checkout co = new Checkout(standardPricing, volumeDiscount);
            for (int c =0; c < gotAmount; c++)
                co.Add(discountedItem.Key);

            co.Total().Should().Be(standardPricing.First().Value * payFor);
        }
        
        [Fact]
        public void Two_Of_Every_Item_Should_Equal_Twice_Total_Of_All_Pricings()
        {
            var pricingDictionary = GetPricingDictionary();
            
            Checkout co = new Checkout(pricingDictionary);
            foreach (var pricing in pricingDictionary)
            {
                co.Add(pricing.Key);
                co.Add(pricing.Key);
            }
            co.Total().Should().Be(2 *pricingDictionary.Values.Sum(p => p));
        }

        [Fact]
        public void VolumeDiscount_Is_Not_Applied_To_Items_Not_Volume_Discounted()
        {
            Dictionary<string, decimal> standardPricing = GetPricingDictionary();

            var item = standardPricing.First();
            int discountAmount = 3;
            int gotAmount = discountAmount - 1;
            VolumeDiscount<string> volumeDiscount = new VolumeDiscount<string>(item.Key, discountAmount, discountAmount - 1);

            Checkout co = new Checkout(standardPricing, volumeDiscount);
            for (int c =0; c < gotAmount; c++)
                co.Add(item.Key);

            co.Total().Should().Be(standardPricing.First().Value * gotAmount);
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
                throw new ArgumentException("Cannot get nothing for something :-)");
            Item = item;
            Get = get;
            ForPriceOf = forPriceOf;
        }

        public TItem Item { get; }
        public int Get { get; }
        public int ForPriceOf { get; }
    }

    public class Checkout
    {
        private readonly List<string> _items;
        private readonly IDictionary<string, decimal> _stdPricing;
        private readonly VolumeDiscount<string>[] _pricingRules;

        public Checkout(
            IDictionary<string, decimal> stdPricing,
            params VolumeDiscount<string>[] pricingRules)
        {
            _items = new List<string>();
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
            
            var calculation = new CostingCalculation<string>(
                costingItems,
                _pricingRules.ToList(),
                0);

            return CostCalculator.GetCost(calculation).Cost;
        }
        
        public void Add(string item)
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
            Items = items;
            Discounts = discounts;
            Cost = cost;
        }

        public Dictionary<TItem, (decimal, int)> Items { get; }
        public List<VolumeDiscount<TItem>> Discounts { get; }
        public decimal Cost { get; }
    }

    public static class CostCalculator
    {
        public static CostingCalculation<string> GetCost(CostingCalculation<string> c)
        {
            return c.Discounts.Any()
                ? GetCost(ApplyNextDiscount(c))
                : BaseCostAllItems(c);
        }

        private static (int, int) DivMod(int dividend, int divisor)
        {
            return (dividend / divisor, dividend % divisor);
        }

        private static (T, List<T>) HeadAndTail<T>(List<T> l)
        {
            return (l.First(), l.Skip(1).ToList());

        }

        private static CostingCalculation<string> ApplyNextDiscount(CostingCalculation<string> c)
        {
            var (discount, remainingDiscounts) = HeadAndTail(c.Discounts);

            if (c.Items.ContainsKey(discount.Item))
            {
                var (itemCost, itemQuantity) = c.Items[discount.Item];
                var (quotient, remainder) = DivMod(itemQuantity, discount.Get);
                
                c.Items[discount.Item] = (itemCost, remainder);
                var cost = quotient * discount.ForPriceOf * itemCost;

                return new CostingCalculation<string>(
                    c.Items, 
                    remainingDiscounts, 
                    c.Cost + cost);
            }
            return new CostingCalculation<string>(c.Items, remainingDiscounts.ToList(), c.Cost);
        }

        private static CostingCalculation<string> BaseCostAllItems(CostingCalculation<string> c)
        {
            var baseCostTotal = c.Items.Sum(i => i.Value.Item1 * i.Value.Item2);
            return new CostingCalculation<string>(
                new Dictionary<string, (decimal, int)>(), 
                c.Discounts, 
                c.Cost + baseCostTotal);
        }
    }
}
