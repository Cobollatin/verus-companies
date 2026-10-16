using System.Reflection;
using Nager.PublicSuffix;
using Nager.PublicSuffix.Models;
using Nager.PublicSuffix.RuleParsers;
using Nager.PublicSuffix.RuleProviders;

namespace Verus.Domain.Internet;

/// <summary>
/// Splits host names using the Public Suffix List, so <c>shop.example.co.uk</c> yields
/// registrable domain <c>example.co.uk</c> and label <c>example</c>. The list ships as an embedded resource.
/// </summary>
internal static class PublicSuffix
{
    private const string ResourceName = "public_suffix_list.dat";

    private static readonly Lazy<DomainParser> Parser = new(CreateParser, LazyThreadSafetyMode.ExecutionAndPublication);

    public static bool TryParse(string host, out string registrableDomain, out string label)
    {
        registrableDomain = string.Empty;
        label = string.Empty;

        if (
            !Parser.Value.TryParse(host, out var info)
            || info is null
            || string.IsNullOrEmpty(info.RegistrableDomain)
            || string.IsNullOrEmpty(info.Domain)
        )
        {
            return false;
        }

        registrableDomain = info.RegistrableDomain;
        label = info.Domain;
        return true;
    }

    private static DomainParser CreateParser()
    {
        using var stream =
            Assembly.GetExecutingAssembly().GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException($"Embedded resource '{ResourceName}' is missing.");
        using var reader = new StreamReader(stream);

        var rules = new TldRuleParser(TldRuleDivisionFilter.All).ParseRules(reader.ReadToEnd());
        return new DomainParser(new StaticRuleProvider(rules));
    }
}
