using System;

internal static class UrlStructureAnalyzerTests
{
    private static int failures;

    private static void Assert(string name, bool condition)
    {
        if (!condition)
        {
            failures++;
            Console.Error.WriteLine("FAILED [{0}]", name);
        }
    }

    private static void Run()
    {
        UrlStructureResult standard = UrlStructureAnalyzer.Analyze(
            "https://www.example.com/path?source=mail");
        Assert("standard URL", standard.IsValid &&
            standard.HostName == "www.example.com" &&
            standard.RegistrableDomain == "example.com" &&
            standard.HasSubdomains);

        UrlStructureResult domainWithPort = UrlStructureAnalyzer.Analyze(
            "example.com:8443/login");
        Assert("domain without scheme and with port", domainWithPort.IsValid &&
            domainWithPort.HasUnusualPort);

        UrlStructureResult multiLabelSuffix = UrlStructureAnalyzer.Analyze(
            "https://login.example.co.uk/");
        Assert("common multi-label public suffix", multiLabelSuffix.IsValid &&
            multiLabelSuffix.RegistrableDomain == "example.co.uk");

        UrlStructureResult ipAddress = UrlStructureAnalyzer.Analyze(
            "http://192.0.2.10/login");
        Assert("IPv4 address", ipAddress.IsValid && ipAddress.IsIpAddress &&
            ipAddress.RegistrableDomain == null &&
            ipAddress.Findings.Exists(finding => finding.IndexOf("HTTP", StringComparison.Ordinal) >= 0));

        UrlStructureResult ipv6Address = UrlStructureAnalyzer.Analyze("https://[2001:db8::1]/");
        Assert("IPv6 address", ipv6Address.IsValid && ipv6Address.IsIpAddress);

        UrlStructureResult shortener = UrlStructureAnalyzer.Analyze("https://bit.ly/example");
        Assert("known URL shortener", shortener.IsValid && shortener.IsShortener);

        UrlStructureResult misleadingSubdomain = UrlStructureAnalyzer.Analyze(
            "https://paypal.com.attacker.example/");
        Assert("brand in misleading subdomain", misleadingSubdomain.IsValid &&
            misleadingSubdomain.HasMisleadingSubdomain);

        UrlStructureResult asciiLookalike = UrlStructureAnalyzer.Analyze("https://paypa1.com/");
        Assert("ASCII brand lookalike", asciiLookalike.IsValid &&
            asciiLookalike.HasPossibleBrandImitation);

        UrlStructureResult unicodeLookalike = UrlStructureAnalyzer.Analyze(
            "https://\u0440\u0430ypal.com/");
        Assert("Unicode brand lookalike", unicodeLookalike.IsValid &&
            unicodeLookalike.IsInternationalized &&
            unicodeLookalike.HasPossibleBrandImitation &&
            unicodeLookalike.HasUnicodeLookalikes);

        UrlStructureResult legitimateIdn = UrlStructureAnalyzer.Analyze(
            "https://b\u00fccher.de/");
        Assert("internationalized domain", legitimateIdn.IsValid &&
            legitimateIdn.IsInternationalized);

        UrlStructureResult userInfo = UrlStructureAnalyzer.Analyze(
            "https://paypal.com@attacker.example/");
        Assert("userinfo is not mistaken for hostname", userInfo.IsValid &&
            userInfo.HasUserInfo && userInfo.HostName == "attacker.example");

        UrlStructureResult malformed = UrlStructureAnalyzer.Analyze("https://[invalid/");
        Assert("malformed address", !malformed.IsValid &&
            malformed.Recommendations.Count > 0);

        UrlStructureResult unsupportedScheme = UrlStructureAnalyzer.Analyze("ftp://example.com/");
        Assert("unsupported scheme", !unsupportedScheme.IsValid);

        UrlStructureResult scriptScheme = UrlStructureAnalyzer.Analyze("javascript:alert(1)");
        Assert("script scheme", !scriptScheme.IsValid);

        UrlStructureResult embeddedWhitespace = UrlStructureAnalyzer.Analyze(
            "https://example .com/");
        Assert("embedded whitespace", !embeddedWhitespace.IsValid);

        UrlStructureResult limitations = UrlStructureAnalyzer.Analyze("example.com");
        Assert("partial suffix and safety limitations are disclosed",
            limitations.Limitations.Count >= 2 &&
            limitations.Limitations.Exists(text =>
                text.IndexOf("lista local parcial", StringComparison.OrdinalIgnoreCase) >= 0) &&
            limitations.Limitations.Exists(text =>
                text.IndexOf("não garante", StringComparison.OrdinalIgnoreCase) >= 0));
    }

    private static int Main()
    {
        Run();
        Console.WriteLine("URL analyzer tests: failures: {0}.", failures);
        return failures == 0 ? 0 : 1;
    }
}
