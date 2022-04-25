using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using DnsClient;
using DnsClient.Protocol;
using McMaster.Extensions.CommandLineUtils;

namespace SPFCheck
{
    public class Program
    {
        public static int Main(string[] args)
        {
            List<string> domainList = new List<string>();
            List<string> checkFor = new List<string>();

            string DomainContainsSPF(string txt)
            {
                foreach (var checkItem in checkFor)
                {
                    if (txt.Contains(checkItem))
                        return checkItem;
                }
                return null;
            }

            var app = new CommandLineApplication();
            app.Name = nameof(SPFCheck);
            app.ResponseFileHandling = ResponseFileHandling.ParseArgsAsSpaceSeparated;
            app.HelpOption();

            var optionFileSource = app.Option("-f|--file <filepath>", "a file containing 1 domain per line to process", CommandOptionType.SingleOrNoValue);
            var optionDomain = app.Option("-d|--domain <domain>", "a domain to process", CommandOptionType.SingleOrNoValue);
            var optionSpf = app.Option("--spf <spfmatch>", "a domain or dns to match in the spf for the given domain(s)", CommandOptionType.MultipleValue);
            var optionVersion = app.Option("--version", "display version number", CommandOptionType.NoValue);
            app.OnExecute(() => {

                if(optionVersion.HasValue())
                {
                    var assembly = Assembly.GetExecutingAssembly();
                    var version = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;
                    Console.WriteLine($"{version}");
                    return;
                }
                if (optionFileSource.HasValue() && File.Exists(optionFileSource.Value()))
                {
                    domainList.AddRange(File.ReadAllLines(optionFileSource.Value()));
                }
                if(optionDomain.HasValue())
                {
                    domainList.Add(optionDomain.Value());
                }
                if (optionSpf.HasValue())
                {
                    checkFor.AddRange(optionSpf.Values);
                }
                if(domainList.Count == 0 || checkFor.Count==0) {
                    Console.WriteLine("No or empty list of domains of spf supplied");
                    return;
                }

                foreach (string domain in domainList)
                {
                    var domainT = domain.TrimEnd();
                    Console.Write($"Check domain: {domainT}: ");
                    var lookup = new LookupClient();
                    var TxtInfo = lookup.Query(domainT, QueryType.TXT).Answers.OfType<TxtRecord>().Where(r => r.Text.FirstOrDefault().Contains("spf1"));
                    switch (TxtInfo.Count())
                    {
                        case 0:
                            Console.WriteLine("no SPF records found on domain!!!");
                            break;
                        case 1:
                            foreach (var info in TxtInfo)
                            {
                                string txt = info.Text.FirstOrDefault();
                                string spf = DomainContainsSPF(txt);
                                if (spf != null)
                                {
                                    Console.WriteLine($"SPF found: {spf}");
                                }
                                else
                                {
                                    Console.WriteLine($"missing record in SPF: {txt}");
                                }
                            }
                            break;
                        default:
                            Console.WriteLine("more than 1 SPF found on domain, bad config!!!");
                            break;
                    };
                    Console.WriteLine();
                }               
            });
            try
            {
                return app.Execute(args);
            }
            catch(UnrecognizedCommandParsingException ex)
            {
                Console.WriteLine($"Error in commandline parameters: {ex.Message}");
            }
            return -1;
        }
    }
}
