using System.Collections.Generic;
using System.Linq;
using AdCheckout.Costing;
using AdCheckout.Costing.Rules;
using FluentAssertions;
using Xunit;

namespace AdCheckoutTests
{
    public class CheckoutTests
    {
        public static List<IPricingRule<string>> OnlyRetailPriceRules()
        {
            return new List<IPricingRule<string>>
            {
                new PerItemCosting<string>("item1", 1.10m),
                new PerItemCosting<string>("item2", 2.30m),
                new PerItemCosting<string>("item3", 5.00m)
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
                new PerItemCosting<string>(item, price)
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
                    .OfType<PerItemCosting<string>>()
                    .Select(r => (r.ProductCode, r.Cost))
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
            var standardItemPrice = stdRules.OfType<PerItemCosting<string>>().First();
            var discountAmount = 3;
            var discountedItem = standardItemPrice.ProductCode;
            
            var volumeDiscount = new NForMDiscount<string>(
                discountedItem, 
                standardItemPrice.Cost,
                discountAmount,
                discountAmount-1);

            var pricingRules = new List<IPricingRule<string>> {volumeDiscount};
            pricingRules.AddRange(stdRules);

            var checkout = new Checkout<string>(pricingRules);
            for (var c =0; c < checkoutItemCount; c++)
                checkout.Add(discountedItem);

            checkout.Total().Should().Be(standardItemPrice.Cost * payingForItemCount);
        }
    }
}