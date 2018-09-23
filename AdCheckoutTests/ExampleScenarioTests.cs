using System.IO;
using System.Xml.Schema;
using AdCheckout;
using AdCheckout.Costing;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using Xunit;

namespace AdCheckoutTests
{
    public class ExampleScenarioTests
    {
        private const string DefaultDatabase = @"pricingDb.json";
        private readonly PricingRulesRepository _pricingRulesRepository;

        public ExampleScenarioTests()
        {
            string dbStr = File.ReadAllText(DefaultDatabase);
            JObject db = JObject.Parse(dbStr);
            
            _pricingRulesRepository = 
                new PricingRulesRepository(JsonFileDal.GetPricingRules(db));
        }

        [Fact]
        public void Test1_Default_Customer()
        {
            var defaultPricingRules = _pricingRulesRepository
                .GetPricingRules(PricingRulesRepository.DefaultCustomerCode);
            
            Checkout<string> checkout = new Checkout<string>(defaultPricingRules);
            
            checkout.Add("classic");
            checkout.Add("standout");
            checkout.Add("premium");

            var total = checkout.Total();

            total.Should().Be(987.97m);
        }
        
        [Fact]
        public void Test2_SecondBite_Customer()
        {
            var pricingRules =
                _pricingRulesRepository.GetPricingRules("secondbite");
            Checkout<string> checkout = new Checkout<string>(pricingRules);
            
            checkout.Add("classic");
            checkout.Add("classic");
            checkout.Add("classic");
            checkout.Add("premium");

            var total = checkout.Total();

            total.Should().Be(934.97m);
        }
        
        [Fact]
        public void Test3_AxilCoffeeRoasters_Customer()
        {
            var defaultPricingRules =
                _pricingRulesRepository.GetPricingRules("axilcoffee");
            Checkout<string> checkout = new Checkout<string>(defaultPricingRules);
            
            checkout.Add("standout");
            checkout.Add("standout");
            checkout.Add("standout");
            checkout.Add("premium");

            var total = checkout.Total();

            total.Should().Be(1294.96m);
        }

        [Fact]
        public void Myer_Customer()
        {
            var defaultPricingRules =
                _pricingRulesRepository.GetPricingRules("myer");
            Checkout<string> checkout = new Checkout<string>(defaultPricingRules);
            
            checkout.Add("standout");
            checkout.Add("standout");
            checkout.Add("standout");
            checkout.Add("standout");
            checkout.Add("standout");    // pay for 4
            checkout.Add("standout");    
            checkout.Add("standout");    // plus 2 (6 total)
            
            checkout.Add("premium");
            
            checkout.Add("classic");


            checkout.Total().Should().Be(2597.92m);
        }
    }
}