using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;

internal sealed class UrlStructureResult
{
    public bool IsValid;
    public string ErrorMessage;
    public string Scheme;
    public string HostName;
    public string RegistrableDomain;
    public bool IsIpAddress;
    public bool IsShortener;
    public bool HasSubdomains;
    public bool HasMisleadingSubdomain;
    public bool IsInternationalized;
    public bool HasUnicodeLookalikes;
    public bool HasPossibleBrandImitation;
    public bool HasUserInfo;
    public bool HasUnusualPort;
    public bool HasUnusualStructure;
    public readonly List<string> PossibleBrands = new List<string>();
    public readonly List<string> Findings = new List<string>();
    public readonly List<string> Recommendations = new List<string>();
    public readonly List<string> Limitations = new List<string>();
}

internal static class UrlStructureAnalyzer
{
    private sealed class Brand
    {
        public readonly string Root;
        public readonly string Name;

        public Brand(string root, string name)
        {
            Root = root;
            Name = name;
        }
    }

    private static readonly Brand[] Brands =
    {
        new Brand("santander", "Santander"),
        new Brand("millennium", "Millennium"),
        new Brand("bpi", "BPI"),
        new Brand("activobank", "ActivoBank"),
        new Brand("novobanco", "novobanco"),
        new Brand("cgd", "CGD"),
        new Brand("paypal", "PayPal"),
        new Brand("microsoft", "Microsoft"),
        new Brand("apple", "Apple"),
        new Brand("google", "Google"),
        new Brand("amazon", "Amazon"),
        new Brand("netflix", "Netflix"),
        new Brand("ctt", "CTT"),
        new Brand("dhl", "DHL"),
        new Brand("ups", "UPS"),
        new Brand("fedex", "FedEx"),
        new Brand("correios", "Correios"),
        new Brand("bancodobrasil", "Banco do Brasil"),
        new Brand("itau", "Itaú"),
        new Brand("nubank", "Nubank")
    };

    private static readonly HashSet<string> ShortenerDomains =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "bit.ly", "tinyurl.com", "t.co", "goo.gl", "is.gd", "cutt.ly",
            "ow.ly", "rebrand.ly", "rb.gy", "shorturl.at", "buff.ly",
            "lnkd.in", "tiny.cc", "soo.gd", "s.id"
        };

    private static readonly HashSet<string> CommonMultiLabelPublicSuffixes =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "ac.uk", "co.uk", "gov.uk", "ltd.uk", "me.uk", "net.uk",
            "org.uk", "plc.uk", "police.uk", "sch.uk",
            "asn.au", "com.au", "edu.au", "gov.au", "id.au", "net.au", "org.au",
            "ac.nz", "co.nz", "govt.nz", "net.nz", "org.nz", "school.nz",
            "ac.jp", "co.jp", "go.jp", "ne.jp", "or.jp",
            "com.br", "edu.br", "gov.br", "net.br", "org.br",
            "com.pt", "edu.pt", "gov.pt", "net.pt", "org.pt",
            "com.cn", "edu.cn", "gov.cn", "net.cn", "org.cn",
            "com.hk", "edu.hk", "gov.hk", "idv.hk", "net.hk", "org.hk",
            "com.sg", "edu.sg", "gov.sg", "net.sg", "org.sg",
            "com.my", "edu.my", "gov.my", "net.my", "org.my",
            "com.mx", "gob.mx", "net.mx", "org.mx",
            "co.in", "firm.in", "gen.in", "ind.in", "net.in", "org.in",
            "co.za", "gov.za", "net.za", "org.za",
            "com.ar", "net.ar", "org.ar",
            "com.tr", "gov.tr", "net.tr", "org.tr",
            "com.tw", "net.tw", "org.tw",
            "com.sa", "net.sa", "org.sa"
        };

    private static readonly Dictionary<char, char> ConfusableCharacters =
        new Dictionary<char, char>
        {
            { '\u0430', 'a' }, { '\u0435', 'e' }, { '\u043e', 'o' },
            { '\u0440', 'p' }, { '\u0441', 'c' }, { '\u0445', 'x' },
            { '\u0443', 'y' }, { '\u0456', 'i' }, { '\u04cf', 'l' },
            { '\u043a', 'k' }, { '\u043c', 'm' }, { '\u0442', 't' },
            { '\u0432', 'b' }, { '\u043d', 'h' },
            { '\u03b1', 'a' }, { '\u03b5', 'e' }, { '\u03bf', 'o' },
            { '\u03c1', 'p' }, { '\u03c7', 'x' }, { '\u03b9', 'i' }
        };

    public static UrlStructureResult Analyze(string address)
    {
        UrlStructureResult result = new UrlStructureResult();
        if (String.IsNullOrWhiteSpace(address))
        {
            return Invalid(result, "Introduza um endereço para analisar.");
        }

        string trimmedAddress = address.Trim();
        foreach (char character in trimmedAddress)
        {
            if (Char.IsControl(character) || Char.IsWhiteSpace(character))
            {
                return Invalid(result, "O endereço contém espaços ou caracteres de controlo.");
            }
        }

        bool hasExplicitScheme = HasExplicitScheme(trimmedAddress);
        string parseAddress;
        if (trimmedAddress.StartsWith("//", StringComparison.Ordinal))
        {
            parseAddress = "https:" + trimmedAddress;
        }
        else if (hasExplicitScheme)
        {
            int schemeEnd = trimmedAddress.IndexOf(':');
            if (!trimmedAddress.Substring(schemeEnd).StartsWith("://", StringComparison.Ordinal))
            {
                return Invalid(result, "O endereço deve incluir um hostname após http:// ou https://.");
            }
            parseAddress = trimmedAddress;
        }
        else
        {
            parseAddress = "https://" + trimmedAddress;
        }

        Uri parsed;
        if (!Uri.TryCreate(parseAddress, UriKind.Absolute, out parsed) ||
            String.IsNullOrWhiteSpace(parsed.Host))
        {
            return Invalid(result, "Não foi possível interpretar este endereço. Verifique o formato e tente novamente.");
        }
        if (!String.Equals(parsed.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) &&
            !String.Equals(parsed.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            return Invalid(result, "Apenas endereços HTTP e HTTPS podem ser analisados.");
        }
        if (parsed.HostNameType == UriHostNameType.Unknown)
        {
            return Invalid(result, "O hostname não tem um formato reconhecido.");
        }

        string host = parsed.IdnHost.TrimEnd('.').ToLowerInvariant();
        if (host.Length == 0)
        {
            return Invalid(result, "O endereço não contém um hostname válido.");
        }

        IPAddress parsedIp;
        result.IsIpAddress = IPAddress.TryParse(host, out parsedIp) ||
            parsed.HostNameType == UriHostNameType.IPv4 ||
            parsed.HostNameType == UriHostNameType.IPv6;
        int registrableStart = -1;
        int suffixLength;
        if (!result.IsIpAddress && !HasValidDnsLabels(host))
        {
            return Invalid(result, "O hostname contém uma ou mais etiquetas inválidas.");
        }

        result.IsValid = true;
        result.Scheme = parsed.Scheme.ToLowerInvariant();
        result.HostName = host;
        result.HasUserInfo = !String.IsNullOrEmpty(parsed.UserInfo);
        result.HasUnusualPort = !parsed.IsDefaultPort &&
            parsed.Port != (result.Scheme == Uri.UriSchemeHttps ? 443 : 80);
        result.IsInternationalized = host.IndexOf("xn--", StringComparison.OrdinalIgnoreCase) >= 0;

        if (result.IsIpAddress)
        {
            result.RegistrableDomain = null;
        }
        else
        {
            result.RegistrableDomain = GetRegistrableDomain(host, out registrableStart, out suffixLength);
            result.HasSubdomains = registrableStart > 0;
            try
            {
                if (result.RegistrableDomain != null)
                {
                    result.IsShortener = ShortenerDomains.Contains(result.RegistrableDomain);
                    FindBrandSignals(host, registrableStart, result);
                }
            }
            catch (ArgumentException)
            {
                return Invalid(result, "O hostname contém uma codificação internacional inválida.");
            }
        }

        bool hasBackslash = trimmedAddress.IndexOf('\\') >= 0;
        bool hasTrailingDot = parsed.DnsSafeHost.EndsWith(".", StringComparison.Ordinal);
        int labelCount = result.IsIpAddress ? 0 : host.Split('.').Length;
        result.HasUnusualStructure = result.HasUserInfo || result.HasUnusualPort ||
            hasBackslash || hasTrailingDot || labelCount > 5;

        AddFindings(result, hasBackslash, hasTrailingDot, labelCount);
        AddRecommendations(result);
        result.Limitations.Add(
            "A análise é apenas estrutural: não consulta reputação, DNS, certificados nem o conteúdo do site, e não abre o endereço.");
        result.Limitations.Add(
            "O domínio registável é estimado com uma lista local parcial de sufixos públicos comuns; domínios menos comuns podem ser interpretados de forma incompleta.");
        result.Limitations.Add(
            "A ausência de sinais estruturais não garante que o endereço ou o site sejam seguros ou legítimos.");
        return result;
    }

    private static bool HasValidDnsLabels(string host)
    {
        if (host.Length > 253)
        {
            return false;
        }
        string[] labels = host.Split('.');
        foreach (string label in labels)
        {
            if (label.Length == 0 || label.Length > 63 ||
                label[0] == '-' || label[label.Length - 1] == '-')
            {
                return false;
            }
            foreach (char character in label)
            {
                if (!Char.IsLetterOrDigit(character) && character != '-')
                {
                    return false;
                }
            }
        }
        return true;
    }

    private static UrlStructureResult Invalid(UrlStructureResult result, string message)
    {
        result.IsValid = false;
        result.ErrorMessage = message;
        result.Recommendations.Add(
            "Confirme o endereço através de uma fonte oficial conhecida antes de o utilizar.");
        result.Limitations.Add(
            "Não foi feita qualquer ligação ao endereço nem pedido a um servidor.");
        return result;
    }

    private static bool HasExplicitScheme(string address)
    {
        int colon = address.IndexOf(':');
        if (colon <= 0)
        {
            return false;
        }
        int slash = address.IndexOfAny(new[] { '/', '?', '#' });
        if (slash >= 0 && slash < colon)
        {
            return false;
        }
        if (colon + 1 < address.Length && Char.IsDigit(address[colon + 1]))
        {
            int position = colon + 1;
            while (position < address.Length && Char.IsDigit(address[position]))
            {
                position++;
            }
            if (position == address.Length || address[position] == '/' ||
                address[position] == '?' || address[position] == '#')
            {
                return false;
            }
        }

        if (!Char.IsLetter(address[0]))
        {
            return false;
        }
        for (int index = 1; index < colon; index++)
        {
            char character = address[index];
            if (!Char.IsLetterOrDigit(character) && character != '+' && character != '-' && character != '.')
            {
                return false;
            }
        }
        return true;
    }

    private static string GetRegistrableDomain(
        string host,
        out int registrableStart,
        out int publicSuffixLength)
    {
        string[] labels = host.Split('.');
        publicSuffixLength = 1;
        for (int suffixLength = 2; suffixLength <= Math.Min(4, labels.Length); suffixLength++)
        {
            string suffix = String.Join(".", labels.Skip(labels.Length - suffixLength).ToArray());
            if (CommonMultiLabelPublicSuffixes.Contains(suffix))
            {
                publicSuffixLength = suffixLength;
            }
        }

        registrableStart = labels.Length - publicSuffixLength - 1;
        if (registrableStart < 0)
        {
            return null;
        }
        return String.Join(".", labels.Skip(registrableStart).ToArray());
    }

    private static void FindBrandSignals(
        string host,
        int registrableStart,
        UrlStructureResult result)
    {
        string[] labels = host.Split('.');
        int domainLabelIndex = registrableStart < 0 ? labels.Length - 1 : registrableStart;
        if (domainLabelIndex < 0 || domainLabelIndex >= labels.Length)
        {
            return;
        }

        bool unicodeConfusable;
        List<string> matches = MatchBrands(labels[domainLabelIndex], out unicodeConfusable);
        string domainSkeleton = GetSkeleton(labels[domainLabelIndex]);
        bool exactBrandRoot = Brands.Any(brand =>
            String.Equals(domainSkeleton, brand.Root, StringComparison.Ordinal));
        if (matches.Count > 0 &&
            (!exactBrandRoot ||
                !String.Equals(domainSkeleton, labels[domainLabelIndex], StringComparison.Ordinal)))
        {
            result.HasPossibleBrandImitation = true;
            result.PossibleBrands.AddRange(matches);
            result.HasUnicodeLookalikes = unicodeConfusable;
        }

        int subdomainEnd = registrableStart;
        for (int index = 0; index < subdomainEnd; index++)
        {
            matches = MatchBrands(labels[index], out unicodeConfusable);
            if (matches.Count == 0)
            {
                continue;
            }

            result.HasMisleadingSubdomain = true;
            foreach (string brand in matches)
            {
                if (!result.PossibleBrands.Contains(brand))
                {
                    result.PossibleBrands.Add(brand);
                }
            }
            result.HasUnicodeLookalikes = result.HasUnicodeLookalikes || unicodeConfusable;
        }
    }

    private static List<string> MatchBrands(string domainLabel, out bool unicodeConfusable)
    {
        string skeleton = GetSkeleton(domainLabel, out unicodeConfusable);
        string[] tokens = skeleton.Split(new[] { '-', '_' }, StringSplitOptions.RemoveEmptyEntries);
        List<string> matches = new List<string>();
        foreach (Brand brand in Brands)
        {
            bool matchesBrand = false;
            foreach (string token in tokens)
            {
                if (String.Equals(token, brand.Root, StringComparison.Ordinal) ||
                    IsOneEditAway(token, brand.Root))
                {
                    matchesBrand = true;
                    break;
                }
            }
            if (matchesBrand && !matches.Contains(brand.Name))
            {
                matches.Add(brand.Name);
            }
        }
        return matches;
    }

    private static string GetSkeleton(string label)
    {
        bool ignored;
        return GetSkeleton(label, out ignored);
    }

    private static string GetSkeleton(string asciiLabel, out bool unicodeConfusable)
    {
        string unicodeLabel = asciiLabel;
        if (asciiLabel.StartsWith("xn--", StringComparison.OrdinalIgnoreCase))
        {
            unicodeLabel = new IdnMapping().GetUnicode(asciiLabel);
        }

        unicodeConfusable = false;
        string lower = unicodeLabel.ToLowerInvariant();
        System.Text.StringBuilder skeleton = new System.Text.StringBuilder(lower.Length);
        foreach (char character in lower)
        {
            char mapped;
            if (ConfusableCharacters.TryGetValue(character, out mapped))
            {
                skeleton.Append(mapped);
                unicodeConfusable = true;
            }
            else if (character == '0')
            {
                skeleton.Append('o');
            }
            else if (character == '1')
            {
                skeleton.Append('l');
            }
            else if (character == '3')
            {
                skeleton.Append('e');
            }
            else if (character == '5')
            {
                skeleton.Append('s');
            }
            else
            {
                skeleton.Append(character);
            }
        }
        return skeleton.ToString();
    }

    private static bool IsOneEditAway(string first, string second)
    {
        if (Math.Abs(first.Length - second.Length) > 1)
        {
            return false;
        }

        int firstIndex = 0;
        int secondIndex = 0;
        int differences = 0;
        while (firstIndex < first.Length && secondIndex < second.Length)
        {
            if (first[firstIndex] == second[secondIndex])
            {
                firstIndex++;
                secondIndex++;
                continue;
            }
            differences++;
            if (differences > 1)
            {
                return false;
            }
            if (first.Length >= second.Length)
            {
                firstIndex++;
            }
            if (second.Length >= first.Length)
            {
                secondIndex++;
            }
        }
        if (firstIndex < first.Length || secondIndex < second.Length)
        {
            differences++;
        }
        return differences == 1;
    }

    private static void AddFindings(
        UrlStructureResult result,
        bool hasBackslash,
        bool hasTrailingDot,
        int labelCount)
    {
        if (result.Scheme == Uri.UriSchemeHttp)
        {
            result.Findings.Add("O endereço usa HTTP, que não cifra a ligação como HTTPS.");
        }
        if (result.IsIpAddress)
        {
            result.Findings.Add("O hostname é um endereço IP, em vez de um nome de domínio.");
        }
        if (result.IsShortener)
        {
            result.Findings.Add("O domínio pertence a um serviço de encurtamento e oculta o destino final.");
        }
        if (result.HasMisleadingSubdomain)
        {
            result.Findings.Add(
                "O hostname inclui o nome de uma marca antes do domínio registável; essa parte não identifica o proprietário do domínio.");
        }
        if (result.HasPossibleBrandImitation)
        {
            result.Findings.Add(
                "O nome do domínio registável é parecido com o de uma marca conhecida: " +
                String.Join(", ", result.PossibleBrands.ToArray()) + ".");
        }
        if (result.IsInternationalized)
        {
            result.Findings.Add("O hostname usa um domínio internacionalizado (IDN), apresentado internamente em formato ASCII.");
        }
        if (result.HasUnicodeLookalikes)
        {
            result.Findings.Add(
                "Foram encontrados caracteres Unicode ou substituições visuais que podem fazer o hostname parecer-se com o de uma marca.");
        }
        if (result.HasUserInfo)
        {
            result.Findings.Add("O endereço inclui credenciais antes do hostname; essa estrutura pode ocultar o destino real.");
        }
        if (result.HasUnusualPort)
        {
            result.Findings.Add("O endereço usa uma porta diferente das portas Web habituais.");
        }
        if (hasBackslash || hasTrailingDot || labelCount > 5)
        {
            result.Findings.Add("O endereço tem uma estrutura menos habitual e deve ser confirmado com atenção.");
        }
        if (result.RegistrableDomain == null && !result.IsIpAddress)
        {
            result.Findings.Add("Não foi possível estimar um domínio registável para este hostname.");
        }
        if (result.Findings.Count == 0)
        {
            result.Findings.Add(
                "Não foram encontrados os indicadores estruturais analisados; isto não confirma que o endereço seja seguro.");
        }
    }

    private static void AddRecommendations(UrlStructureResult result)
    {
        result.Recommendations.Add(
            "Confirme a identidade e o domínio através da aplicação oficial ou de um endereço que já conheça, digitado manualmente.");
        result.Recommendations.Add(
            "Não introduza palavras-passe, códigos ou dados bancários depois de seguir um link inesperado.");
        if (result.IsShortener || result.HasMisleadingSubdomain ||
            result.HasPossibleBrandImitation || result.IsIpAddress)
        {
            result.Recommendations.Add(
                "Não use o link até confirmar o destino por um canal independente.");
        }
    }

}
