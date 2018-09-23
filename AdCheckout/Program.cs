using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AdCheckout.Costing;
using CommandLine;
using Newtonsoft.Json.Linq;

namespace AdCheckout
{
    class Program
    {
        static void Main(string[] args)
        {
            
            CommandLine.Parser.Default.ParseArguments<Options>(args)
                .WithParsed<Options>(opts => RunOptionsAndReturnExitCode(opts))
                .WithNotParsed<Options>((errs) => HandleParseError(errs));
        }
        
        
        private static void HandleParseError(IEnumerable<Error> errs)
        {
            Console.Write(String.Join("\n", errs));
        }

        private const string DefaultDatabase = @"pricingDb.json";
        
        private static void RunOptionsAndReturnExitCode(Options opts)
        {
            var items = opts.Items.ToList();
            var customer = opts.Customer;
            
            string dbStr = File.ReadAllText(DefaultDatabase);
            JObject db = JObject.Parse(dbStr);
            var pricingRulesRepository = new PricingRulesRepository(JsonFileDal.GetPricingRules(db));
            var pricingRules = pricingRulesRepository.GetPricingRules(customer);
            Checkout<string> checkout = new Checkout<string>(pricingRules);

            foreach (var item in items)
            {
                checkout.Add(item);
            }

            var total = checkout.Total();
            
            Console.WriteLine($"Customer: {customer}");
            Console.WriteLine($"Items: {String.Join(", ", items)}");
            Console.WriteLine($"Total: {total}");
        }
    }
    
    
    public class Options
    {
        [Option('p', "product", Required=true, HelpText="Products sold. Try: classic, standout, premium")]
        public IEnumerable<string> Items { get; set; }
        
        [Option('c', "customer", Required=true, HelpText="Customer to price against (try 'default' if stuck)")]
        public string Customer { get; set; }
    }
}