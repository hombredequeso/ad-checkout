using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Xunit;

namespace AdCheckoutTests2
{
    public class CheckoutTests2
    {
        public static List<IPricingRule<string>> OnlyRetailPriceRules()
        {
            return new List<IPricingRule<string>>
            {
                new RetailCost<string>("item1", 1.10m),
                new RetailCost<string>("item2", 2.30m),
                new RetailCost<string>("item3", 5.00m)
            };
            
        }

        [Fact]
        public void No_Items_Totals_Zero()
        {
            new Checkout<string>(OnlyRetailPriceRules())
                .Total()
                .Should().Be(0);
        }
        
        
        [Fact]
        public void One_Item_No_Rules_Equals_Item_Price()
        {
            const string item = "item1";
            const decimal price = 269.99m;
            var pricingRules =  new List<IPricingRule<string>>
            {
                new RetailCost<string>(item, price)
            };
            
            var checkout = new Checkout<string>(pricingRules);
            checkout.Add(item);
            
            decimal total = checkout.Total();
            total.Should().Be(price);
        }
        
        [Theory]
        [InlineData(0)]    
        [InlineData(1)]    
        [InlineData(2)]    
        public void Multiples_Of_Every_Item_Should_Cost_Total_Of_All_Items_Times_Multiple(int countOfEachItem)
        {
            List<(string, decimal)> retailPrices =
                OnlyRetailPriceRules()
                    .OfType<RetailCost<string>>()
                    .Select(r => (r.Item, r.ItemCost))
                    .ToList();

            var totalCost = retailPrices.Sum(x => x.Item2);
            
            var checkout = new Checkout<string>(OnlyRetailPriceRules());
            foreach (var pricing in retailPrices)
            {
                for(var c=0;c<countOfEachItem; c++)
                    checkout.Add(pricing.Item1);
            }
            
            decimal total = checkout.Total();
            total.Should().Be(totalCost * countOfEachItem);
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
            List<IPricingRule<string>> stdRules = OnlyRetailPriceRules();
            var standardItemPrice = stdRules.OfType<RetailCost<string>>().First();
            var discountAmount = 3;
            var discountedItem = standardItemPrice.Item;
            
            var volumeDiscount = new NForMDiscount<string>(
                discountedItem, 
                standardItemPrice.ItemCost,
                discountAmount,
                discountAmount-1);

            var pricingRules = new List<IPricingRule<string>> {volumeDiscount};
            pricingRules.AddRange(stdRules);

            var checkout = new Checkout<string>(pricingRules);
            for (var c =0; c < checkoutItemCount; c++)
                checkout.Add(discountedItem);

            checkout.Total().Should().Be(standardItemPrice.ItemCost * payingForItemCount);
        }
    }

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
            var groupedItems = _items.GroupBy(x => x).ToDictionary(i => i.Key, i => i.Count());
            var costingBasket = new CostingBasket<TItem>(groupedItems, 0);
            var result = CostCalculator.ApplyCostings(costingBasket, _pricingRules);
            return result.Cost;
        }
    }


    // Dictionary<TItem, int> basket;
    // decimal cost

    public static class CostCalculator
    {
        public static CostingBasket<TItem> ApplyCostings<TItem>(
            CostingBasket<TItem> basketIn,
            List<IPricingRule<TItem>> rules)
        {
            return rules.Aggregate(
                basketIn, 
                (basket, rule) => rule.ApplyToBasket(basket));
        }
    }

    public interface IPricingRule<TItem>
    {
        CostingBasket<TItem> ApplyToBasket(CostingBasket<TItem> basket);
    }

    public class RetailCost<TItem> : IPricingRule<TItem>
    {
        public RetailCost(TItem item, decimal itemCost)
        {
            Item = item;
            ItemCost = itemCost;
        }

        public TItem Item { get; }
        public decimal ItemCost { get; }
        
        public CostingBasket<TItem> ApplyToBasket(CostingBasket<TItem> basket)
        {
            if (basket.ItemCounts.ContainsKey(Item))
            {
                int items = basket.ItemCounts[Item];
                var cost = items * ItemCost;
                basket.ItemCounts[Item] = 0;
                return new CostingBasket<TItem>(basket.ItemCounts, basket.Cost + cost);
            }
            return basket;
        }
    }

    public class NForMDiscount<TItem>: IPricingRule<TItem>
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

                var itemsOut = new Dictionary<TItem, int>(basket.ItemCounts) {[Item] = remainder};
                var cost = quotient * ForPriceOf * ItemCost;
                return new CostingBasket<TItem>(itemsOut, basket.Cost + cost);
            }
            return basket;
        }
        
        
        private static (int, int) DivMod(int dividend, int divisor)
        {
            return (dividend / divisor, dividend % divisor);
        }
    }
    
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