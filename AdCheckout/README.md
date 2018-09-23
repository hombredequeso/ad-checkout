## Program
To run the program:

```
cd AdCheckout
dotnet run
```

This will provide the available command line options.
Example usages:

```
dotnet run -- -c default -p classic
dotnet run -- -c myer -p premium standout standout standout standout standout
```

## Tests
xunit tests are provided in AdCheckoutTests

```
cd AdCheckoutTests
dotnet test
```

## Program Overview
The type of the item is generic, TItem. For the most part strings are used here.

1. An instance of Checkout<TItem> is created.
A list of IPricingRules is passed to it.
2. Items are added to the checkout.
3. checkout.Total() will give the total price at any time.

### IPricingRules
There are two types of pricing rules:
- PerItemCosting. This is flat cost per item pricing.
- NForMDiscount. This provides a discount, N items for the price of M, with cost as per the PricngRule.

### Costing Process
To cost a basket the following is done:
- the basket contents is turned into a CostingBasket.
- the list of IPricingRules is then applied in sequence.
- The CostingBasket is immutable. Each rule produces a new CostingBasket.

### PricingRuleRepository
- The PricingRuleRepository takes a dictionary of rules, where the key is the customer code.
- One code must be the default customer code ('default').
- GetPricingRules(customerCode) will return the costing rules for the specified customer.
It always adds the default rules onto the end of the rules, for each customer.

### Construction of Pricing Rules.
- The high level pricing rules start in pricingDb.json. Here, the default retail prices for each item are specified (as 'default' customer).
Customers with special pricing arrangements then have their special rules.
- JsonFileDal.GetPricingRules takes pricing rules in JSON format, and turns them into a IPricingRules, suitable to be passed
into the PricingRuleRepository. Here that the pricing format as found in the JSON file gets transformed into pricing rules.

