using System;
using System.Collections.Generic;
using System.Linq;
using AdCheckout.Costing.Rules;
using Newtonsoft.Json.Linq;

namespace AdCheckout
{
    public class JsonFileDal
    {
        public static Dictionary<string, List<IPricingRule<string>>> GetPricingRules(JObject db)
        {
            JArray defaultRetailCosts = (JArray) db[PricingRulesRepository.DefaultCustomerCode];
            List<PerItemCosting<string>> x = defaultRetailCosts.ToObject<List<PerItemCosting<string>>>();
            
            IEnumerable<KeyValuePair<string, List<IPricingRule<string>>>> zz = db
                .Properties()
                .Select(p => new KeyValuePair<string, List<IPricingRule<string>>>(
                    p.Name,
                    ToPricingRules((JArray) p.Value, x)));
            
            Dictionary<string, List<IPricingRule<string>>> pricingDictionary = 
                new Dictionary<string, List<IPricingRule<string>>>(zz);
            
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
                    decimal amount = retailCosts.First(c => c.ProductCode == productCode).Cost;
                    int @get = (int) pricing["get"];
                    int @for = (int) pricing["for"];

                    return new NForMDiscount<string>(productCode, amount, @get, @for);
                
                default: throw new Exception("unknown pricing code");
            }
        }
    }
}