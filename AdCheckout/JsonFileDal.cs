using System;
using System.Collections.Generic;
using System.Linq;
using AdCheckout.Costing.Rules;
using Newtonsoft.Json.Linq;

namespace AdCheckout
{
    public static class JsonFileDal
    {
        public static Dictionary<string, List<IPricingRule<string>>> GetPricingRules(JObject db)
        {
            JArray defaultCustomerRules = 
                (JArray) db[PricingRulesRepository.DefaultCustomerCode];
            List<PerItemCosting<string>> defaultPerItemCostings = 
                defaultCustomerRules.ToObject<List<PerItemCosting<string>>>();
            
            IEnumerable<KeyValuePair<string, List<IPricingRule<string>>>> customerPricingRules = db
                .Properties()
                .Select(p => new KeyValuePair<string, List<IPricingRule<string>>>(
                    p.Name,
                    ToPricingRules((JArray) p.Value, defaultPerItemCostings)));
            
            Dictionary<string, List<IPricingRule<string>>> pricingDictionary = 
                new Dictionary<string, List<IPricingRule<string>>>(customerPricingRules);
            
            return pricingDictionary;
        }

        private static List<IPricingRule<string>> ToPricingRules(
            JArray pricings, 
            List<PerItemCosting<string>> retailCosts)
        {
            var prs =
                from e in pricings
                select ToPricingRule((JObject)e, retailCosts);
            return prs.ToList();
        }

        private static IPricingRule<string> ToPricingRule(
            JObject pricing, 
            List<PerItemCosting<string>> retailCosts)
        {
            switch ((string) pricing["pricing"])
            {
                case "discount": return pricing.ToObject<PerItemCosting<string>>();
                case "retail": return pricing.ToObject<PerItemCosting<string>>();
                case "nForM" :
                    string productCode = (string) pricing["productCode"];
                    decimal amount = pricing.ContainsKey("cost") 
                        ? pricing["cost"].Value<Decimal>()
                        : retailCosts.First(c => c.ProductCode == productCode).Cost;
                    int @get = (int) pricing["get"];
                    int @for = (int) pricing["for"];

                    return new NForMDiscount<string>(productCode, amount, @get, @for);
                
                default: throw new Exception("unknown pricing code");
            }
        }
    }
}