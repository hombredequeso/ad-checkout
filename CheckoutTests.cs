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
            Checkout co = new Checkout();
            decimal total = co.Total();
            total.Should().Be(0);
        }

        [Fact]
        public void One_Item_No_Rules_Equals_Item_Price()
        {
            decimal price = 269.99m;
            Checkout co = new Checkout();
            co.Add(new Item("nameOfItem", price));
            decimal total = co.Total();
            total.Should().Be(price);
        }

        [Fact]
        public void Multiple_Items_No_Rules_Equals_Sum_Of_Prices()
        {
            List<decimal> prices = new List<decimal>(){1.2m, 3.4m, 9.3m};
            List<Item> items = prices.Select(p => new Item(Guid.NewGuid().ToString(), p)).ToList();
            
            Checkout co = new Checkout();
            items.ForEach(i => co.Add(i));

            co.Total().Should().Be(prices.Sum());
        }

        [Fact]
        public void VolumeDiscount_Is_Not_Applied_To_Items_Not_Volume_Discounted()
        {
            List<decimal> prices = new List<decimal>{1.2m, 3.4m, 9.3m};
            List<Item> items = prices
                .Select(p => new Item(Guid.NewGuid().ToString(), p))
                .ToList();
            var differentTypeOfItem = Guid.NewGuid().ToString();
            var discount = new VolumeDiscount(differentTypeOfItem, 2, 3);
            
            Checkout co = new Checkout(discount);
            items.ForEach(i => co.Add(i));

            co.Total().Should().Be(prices.Sum());
        }
        
        
        [Fact]
        public void VolumeDiscount_Is_Applied_To_Items_Volume_Discounted()
        {
            string item = Guid.NewGuid().ToString();
            decimal itemPrice = 10.0m;
            List<Item> items = Enumerable.Range(0, 3)
                .Select(x => new Item(Guid.NewGuid().ToString(), itemPrice))
                .ToList();
            var discount = new VolumeDiscount(item, 2, 3);
            
            Checkout co = new Checkout(discount);
            items.ForEach(i => co.Add(i));

            co.Total().Should().Be(itemPrice * 2);
        }
    }

    public class VolumeDiscount
    {
        public VolumeDiscount(
            string item, 
            int get, 
            int @for)
        {
            Item = item;
            Get = get;
            For = @for;
        }

        public string Item { get; set; }
        public int Get { get; }
        public int For { get; }
    }

    public class Item
    {
        public Item(
            string name, 
            decimal price)
        {
            Name = name;
            Price = price;
        }

        public string Name { get; }
        public decimal Price { get; }
    }

    public class Checkout
    {
        private List<Item> _items;
        private VolumeDiscount[] _pricingRules;

        public Checkout(params VolumeDiscount[] pricingRules)
        {
            _items = new List<Item>();
            _pricingRules = pricingRules;
        }

        public decimal Total()
        {
            var itemsByGroup = _items.GroupBy(i => i.Name).ToList();
            return _items.Sum(i => i.Price);
        }

        public void Add(Item item)
        {
            _items.Add(item);
        }
    }
}