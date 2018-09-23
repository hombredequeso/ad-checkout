using System;
using System.Collections.Generic;
using System.Linq;
using AdCheckout.Costing.Rules;

namespace AdCheckout
{
    public class PricingRulesRepository
    {
        private readonly IDictionary<string, List<IPricingRule<string>>> _customerPricingRules;

        public static readonly string DefaultCustomerCode = "default";
        
        private readonly List<IPricingRule<string>> _defaultRules;
        public PricingRulesRepository(IDictionary<string, List<IPricingRule<string>>> customerPricingRules)
        {
            _customerPricingRules = 
                customerPricingRules 
                ?? throw new ArgumentNullException(nameof(customerPricingRules));
            
            _defaultRules = _customerPricingRules.ContainsKey(DefaultCustomerCode)
                ? _customerPricingRules[DefaultCustomerCode]
                : throw new ArgumentException(
                    $"must contain rules for '{DefaultCustomerCode}'", 
                    nameof(customerPricingRules));

        }
        
        public List<IPricingRule<string>> GetPricingRules(string customerCode)
        {
            if (!_customerPricingRules.ContainsKey(DefaultCustomerCode) 
                || customerCode == DefaultCustomerCode)
                return _defaultRules;
            
            var specificCustomerRules = _customerPricingRules[customerCode];
            return specificCustomerRules.Concat(_defaultRules).ToList();
        }
        
    }
}