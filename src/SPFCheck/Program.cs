using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using DnsClient;
using DnsClient.Protocol;
using McMaster.Extensions.CommandLineUtils;
using Spectre.Console;

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

            (bool found, string message) CheckSPF(string domain)
            {
                var domainT = domain.TrimEnd();
                var lookup = new LookupClient();
                var TxtInfo = lookup.Query(domainT, QueryType.TXT).Answers.OfType<TxtRecord>().Where(r => r.Text.FirstOrDefault().Contains("spf1"));
                switch (TxtInfo.Count())
                {
                    case 0:
                        return (false, "no SPF records found on domain!!!");
                    case 1:
                        var info = TxtInfo.First();
                        string txt = info.Text.FirstOrDefault();
                        string spf = DomainContainsSPF(txt);
                        if (spf != null)
                        {
                            return (true, $"[green]SPF found[/]: [bold]{spf}[/]");
                        }
                        else
                        {
                            return (false, $"[red]SPF missing[/]: {txt}");
                        }
                    default:
                        return (false, "more than 1 SPF found on domain, bad config!!!");
                };
            }
            var app = new CommandLineApplication();
            app.Name = nameof(SPFCheck);
            app.ResponseFileHandling = ResponseFileHandling.ParseArgsAsSpaceSeparated;
            app.HelpOption();

            var optionFileSource = app.Option("-f|--file <filepath>", "a file containing 1 domain per line to process", CommandOptionType.SingleOrNoValue);
            var optionDomain = app.Option("-d|--domain <domain>", "a domain to process", CommandOptionType.MultipleValue);
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
                    domainList.AddRange(optionDomain.Values);
                }
                if (optionSpf.HasValue())
                {
                    checkFor.AddRange(optionSpf.Values);
                }
                if(domainList.Count == 0 || checkFor.Count==0) {
                    AnsiConsole.MarkupLine("No or empty list of domains of spf supplied");
                    return;
                }
                if (domainList.Count == 1)
                {
                    // Single
                    var domainT = domainList.First().TrimEnd();
                    (bool status, string message) = CheckSPF(domainT);
                    AnsiConsole.Markup($"Domain: [bold]{domainT}[/] -- Status: ");
                    if(status)
                    {
                        AnsiConsole.MarkupLine(message);
                    }
                    else
                    {
                        AnsiConsole.MarkupLine(message);
                    }
                    AnsiConsole.WriteLine();
                }
                else
                {
                    var table = new Table();
                    table.AddColumn(new TableColumn(new Markup("Domain")));
                    table.AddColumn(new TableColumn(new Markup("Status")));
                    table.AddColumn(new TableColumn(new Markup("Error")));
                    foreach (string domain in domainList)
                    {
                        var domainT = domain.TrimEnd();
                        (bool status, string message) = CheckSPF(domainT);
                        if(status)
                        {
                            table.AddRow(new Markup(domainT), new Markup("[green]SPF found[/]"), new Markup(""));
                        }
                        else
                        {
                            table.AddRow(new Markup(domainT), new Markup("[red]error[/]"), new Markup(message));
                        }
                    }
                    AnsiConsole.Write(table);
                }
            });
            try
            {
                return app.Execute(args);
            }
            catch(UnrecognizedCommandParsingException ex)
            {
                AnsiConsole.WriteLine($"Error in commandline parameters: {ex.Message}");
            }
            return -1;
        }
    }
}
