using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AdCheckout;
using AdCheckout.Costing.Rules;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using Xunit;

namespace AdCheckoutTests
{
    public class PricingRulesRepositoryTests
    {
        public const string TestDatabaseFile = @"pricingDb.json";
        
        [Fact]
        public void GetPricingRules_For_A_Default_Customer_Returns_Default_Pricing_Rules()
        {
            var defaultPricingRules = 
                new List<IPricingRule<string>>
                {
                    new RetailCost<string>("classic", 269.99m),
                    new RetailCost<string>("standout", 322.99m),
                    new RetailCost<string>("premium", 394.99m)
                };
            IDictionary<string, List<IPricingRule<string>>> _customerPricingRules =
                new Dictionary<string, List<IPricingRule<string>>>()
                {
                    { "default", defaultPricingRules}
                };
            var repository = new PricingRulesRepository(_customerPricingRules);

            repository.GetPricingRules("default")
                .Should().Equal(defaultPricingRules);
        }

        [Fact]
        public void Database_Contains_Customer_Default_Costings()
        {
            string dbStr = File.ReadAllText(TestDatabaseFile);
            JObject db = JObject.Parse(dbStr);
            
            var pricingRulesRepository = new PricingRulesRepository(JsonFileDal.GetPricingRules(db));
            
            var defaultPricingRules = 
                new List<IPricingRule<string>>
                {
                    new RetailCost<string>("classic", 269.99m),
                    new RetailCost<string>("standout", 322.99m),
                    new RetailCost<string>("premium", 394.99m)
                };

            pricingRulesRepository.GetPricingRules(PricingRulesRepository.DefaultCustomerCode)
                .Should().Equal(defaultPricingRules);
        }

        [Fact]
        public void PricingRulesRepository_Returns_Custom_Then_Default_Rules_For_A_Customer()
        {
            string dbStr = File.ReadAllText(TestDatabaseFile);
            JObject db = JObject.Parse(dbStr);
            
            var pricingRulesRepository = new PricingRulesRepository(JsonFileDal.GetPricingRules(db));
            
            var expectedAxilPricingRules = 
                new List<IPricingRule<string>>
                {
                    new RetailCost<string>("standout", 299.99m),
                    new RetailCost<string>("classic", 269.99m),
                    new RetailCost<string>("standout", 322.99m),
                    new RetailCost<string>("premium", 394.99m)
                };

            var result = pricingRulesRepository.GetPricingRules("axilcoffee");
                result.Should().Equal(expectedAxilPricingRules);
            
        }
        

    }
}