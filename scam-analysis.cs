using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

internal enum ScamRiskLevel
{
    Inconclusive,
    Low,
    Moderate,
    High
}

internal sealed class MessageEntity
{
    public string Kind;
    public string Value;
    public int Start;
    public int Length;
    public string Host;
    public bool IsShortener;
    public bool IsIpAddress;
    public bool IsInternationalized;
    public bool IsLookalike;
}

internal sealed class AnalysisEvidence
{
    public string Excerpt;
    public string Explanation;
}

internal sealed class AnalysisSignal
{
    public string Id;
    public string Label;
    public int Weight;
    public readonly List<AnalysisEvidence> Evidence = new List<AnalysisEvidence>();
}

internal sealed class ScamAnalysisResult
{
    public ScamRiskLevel RiskLevel;
    public int Score;
    public bool IsConclusive;
    public string Summary;
    public readonly List<AnalysisSignal> Signals = new List<AnalysisSignal>();
    public readonly List<string> Recommendations = new List<string>();
    public readonly List<string> Limitations = new List<string>();
}

internal sealed class PreparedMessage
{
    private readonly List<int> originalStarts = new List<int>();
    private readonly List<int> originalEnds = new List<int>();

    public string Original { get; private set; }
    public string Normalized { get; private set; }
    public readonly List<MessageEntity> Entities = new List<MessageEntity>();

    public PreparedMessage(string original)
    {
        Original = original ?? "";
        Normalized = NormalizeWithMap(Original);
        FindEntities();
    }

    public string GetOriginalExcerpt(int normalizedStart, int normalizedLength)
    {
        if (normalizedLength <= 0 || normalizedStart < 0 || normalizedStart >= originalStarts.Count)
        {
            return Original;
        }

        int endIndex = Math.Min(originalEnds.Count - 1, normalizedStart + normalizedLength - 1);
        int start = originalStarts[normalizedStart];
        int end = originalEnds[endIndex];
        if (start < 0 || end < start || start >= Original.Length)
        {
            return Original;
        }

        return Original.Substring(start, Math.Min(Original.Length, end) - start).Trim();
    }

    private string NormalizeWithMap(string value)
    {
        StringBuilder normalized = new StringBuilder(value.Length);
        TextElementEnumerator elements = StringInfo.GetTextElementEnumerator(value);
        while (elements.MoveNext())
        {
            int sourceStart = elements.ElementIndex;
            string sourceElement = elements.GetTextElement();
            string decomposed = sourceElement.Normalize(NormalizationForm.FormKC)
                .Normalize(NormalizationForm.FormD);
            StringBuilder cleanElement = new StringBuilder(decomposed.Length);
            foreach (char character in decomposed)
            {
                UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(character);
                if (category != UnicodeCategory.NonSpacingMark &&
                    category != UnicodeCategory.SpacingCombiningMark &&
                    category != UnicodeCategory.EnclosingMark)
                {
                    cleanElement.Append(Char.ToLowerInvariant(character));
                }
            }

            foreach (char character in cleanElement.ToString())
            {
                if (Char.IsWhiteSpace(character))
                {
                    if (normalized.Length == 0 || normalized[normalized.Length - 1] == ' ')
                    {
                        if (normalized.Length > 0)
                        {
                            originalEnds[originalEnds.Count - 1] = sourceStart + sourceElement.Length;
                        }
                        continue;
                    }
                    normalized.Append(' ');
                    originalStarts.Add(sourceStart);
                    originalEnds.Add(sourceStart + sourceElement.Length);
                }
                else
                {
                    normalized.Append(character);
                    originalStarts.Add(sourceStart);
                    originalEnds.Add(sourceStart + sourceElement.Length);
                }
            }
        }

        int leadingSpaces = 0;
        while (leadingSpaces < normalized.Length && normalized[leadingSpaces] == ' ')
        {
            leadingSpaces++;
        }
        int trimmedLength = normalized.Length;
        while (trimmedLength > leadingSpaces && normalized[trimmedLength - 1] == ' ')
        {
            trimmedLength--;
        }
        if (leadingSpaces > 0)
        {
            normalized.Remove(0, leadingSpaces);
            originalStarts.RemoveRange(0, leadingSpaces);
            originalEnds.RemoveRange(0, leadingSpaces);
        }
        if (trimmedLength < normalized.Length)
        {
            int trailingSpaces = normalized.Length - trimmedLength;
            normalized.Remove(trimmedLength, trailingSpaces);
            originalStarts.RemoveRange(trimmedLength, trailingSpaces);
            originalEnds.RemoveRange(trimmedLength, trailingSpaces);
        }
        return normalized.ToString();
    }

    private void FindEntities()
    {
        FindUrls();
        FindMatches("email", new Regex(@"(?<![\w.+-])[\w.+-]+@[\w-]+(?:\.[\w-]+)+",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant));
        FindMatches("phone", new Regex(@"(?<!\w)(?:\+?\d[\d\s().-]{6,}\d)(?!\w)",
            RegexOptions.CultureInvariant));
        FindMatches("payment-reference", new Regex(
            @"\b(?:iban|mb\s*way|pix|iban|swift|bic|referencia|referência|c[oó]digo de pagamento)\b",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant));
    }

    private void FindUrls()
    {
        Regex candidates = new Regex(
            @"(?<![@\w])(?:(?:https?://|www\.)[^\s<>""']+|(?:[\p{L}\p{N}-]+\.)+(?:com|org|net|pt|br|eu|gov|edu|io|co|info|biz|me|app|online|site|xyz|top|cloud|shop|store|dev|ai|xn--)(?:/[^\s<>""']*)?)",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        foreach (Match match in candidates.Matches(Original))
        {
            string candidate = match.Value.TrimEnd('.', ',', ';', ':', '!', '?', ')', ']', '}');
            string normalizedCandidate = candidate.StartsWith("www.", StringComparison.OrdinalIgnoreCase)
                ? "https://" + candidate
                : candidate.IndexOf("://", StringComparison.Ordinal) < 0
                    ? "https://" + candidate
                    : candidate;
            Uri uri;
            if (!Uri.TryCreate(normalizedCandidate, UriKind.Absolute, out uri) ||
                (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                continue;
            }

            string host = uri.IdnHost.TrimEnd('.');
            if (host.Length == 0)
            {
                continue;
            }

            bool ipAddress = uri.HostNameType == UriHostNameType.IPv4 ||
                uri.HostNameType == UriHostNameType.IPv6;
            bool internationalized = host.IndexOf("xn--", StringComparison.OrdinalIgnoreCase) >= 0;
            bool shortener = new[]
            {
                "bit.ly", "tinyurl.com", "t.co", "goo.gl", "is.gd", "cutt.ly",
                "ow.ly", "rebrand.ly", "rb.gy", "shorturl.at"
            }.Contains(host, StringComparer.OrdinalIgnoreCase);
            bool lookalike = internationalized || LooksLikeBrandDomain(host);
            Entities.Add(new MessageEntity
            {
                Kind = "url",
                Value = candidate,
                Start = match.Index,
                Length = candidate.Length,
                Host = host,
                IsShortener = shortener,
                IsIpAddress = ipAddress,
                IsInternationalized = internationalized,
                IsLookalike = lookalike
            });
        }
    }

    private void FindMatches(string kind, Regex expression)
    {
        foreach (Match match in expression.Matches(Original))
        {
            Entities.Add(new MessageEntity
            {
                Kind = kind,
                Value = match.Value,
                Start = match.Index,
                Length = match.Length
            });
        }
    }

    private static bool LooksLikeBrandDomain(string host)
    {
        string[] brandRoots =
        {
            "santander", "millennium", "bpi", "activobank", "novobanco", "cgd",
            "paypal", "microsoft", "apple", "google", "amazon", "netflix",
            "ctt", "dhl", "ups", "fedex", "correios", "bancodobrasil", "itau", "nubank"
        };
        string asciiHost;
        try
        {
            asciiHost = new IdnMapping().GetAscii(host).ToLowerInvariant();
        }
        catch (ArgumentException)
        {
            return true;
        }

        string[] labels = asciiHost.Split('.');
        for (int index = 0; index < labels.Length; index++)
        {
            string label = labels[index];
            if (label.StartsWith("xn--", StringComparison.Ordinal))
            {
                continue;
            }

            string skeleton = label
                .Replace('0', 'o')
                .Replace('1', 'l')
                .Replace('3', 'e')
                .Replace('5', 's');
            foreach (string brand in brandRoots)
            {
                if (skeleton == brand)
                {
                    string registrablePart = String.Join(".", labels.Skip(Math.Max(0, labels.Length - 2)).ToArray());
                    string trustedRoot = brand + "." + labels[labels.Length - 1];
                    if (!String.Equals(registrablePart, trustedRoot, StringComparison.Ordinal))
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }
}

internal static class ScamAnalyzer
{
    internal const int MaximumMessageLength = 20000;

    private sealed class Rule
    {
        public string Id;
        public string Label;
        public int Weight;
        public Regex Expression;
        public string Explanation;
    }

    private static readonly Regex SentenceSplit = new Regex(@"(?<=[.!?;])\s+|\r?\n+",
        RegexOptions.CultureInvariant);
    private static readonly Regex RequestVerb = new Regex(
        @"\b(?:envie|manda(?:-me)?|mande|compartilhe|partilhe|informe|digite|introduza|forne[cç]a|passe|diga|responda com|envia|env[ií]a|env[ií]e|compartilh[e]?|informe|precisamos|solicitamos|pedimos|necessitamos|send|share|forward|type|provide|tell me)\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly Regex NegationOrWarning = new Regex(
        @"\b(?:n[aã]o (?:partilhe|compartilhe|envie|informe|fornec[aç]a|passe|revele|divulgue|clique|aceda|responda)|nunca (?:partilhe|compartilhe|envie|fornec[aç]a|revele)|jamais (?:compartilhe|envie|informe)|o banco nunca pede|banco nunca (?:pede|solicita)|n[aã]o pedimos|alerta(?: de)? (?:burla|fraude|golpe)|aviso educativo|dica de seguran[cç]a|cuidado com|aviso sobre|para sua seguran[cç]a.*n[aã]o|recebi uma mensagem que dizia|a mensagem dizia|o texto dizia|este aviso explica como (?:identificar|detetar|proteger|prevenir).{0,40}(?:phishing|fraude|burla|golpe))\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly Regex ReportedMessageContext = new Regex(
        @"\b(?:recebi uma mensagem que dizia|a mensagem dizia|o texto dizia)\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly Regex ContrastMarker = new Regex(
        @"\b(?:mas|por[eé]m|contudo)\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private const string PaymentActionPattern =
        @"envi(?:e|a|ar|ando|nando|am|em)(?:-me)?|manda(?:-me)?|mande(?:-me)?|mandem|mandar|transfira(?:-me)?|transfere|transfiram|transferr?(?:ir|indo|e|a|am|em)|trasnfer(?:ir|indo)|pague|paga|paguem|pagar|pagando|deposite(?:-me)?|deposita|depositem|depositar";
    private const string MoneyAmountPattern =
        @"[$€£]\s*\d+(?:[.,]\d{1,3})*|\d+(?:[.,]\d{1,3})*\s*(?:[$€£]|\b(?:usd|eur|brl|gbp)\b)|\d+(?:[.,]\d{1,3})*\s+\b(?:d[oó]lares?|euros?|reais?)\b";
    private const string PaymentActionAndAmountPattern =
        @"\b(?:" + PaymentActionPattern + @")\b.{0,35}(?:" + MoneyAmountPattern +
        @")|(?:" + MoneyAmountPattern + @").{0,35}\b(?:" + PaymentActionPattern + @")\b";
    private static readonly Regex PaymentActionAndAmount = new Regex(
        PaymentActionAndAmountPattern,
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly Regex PaymentCondition = new Regex(
        @"\b(?:para|pra)\s+(?:poder\s+)?(?:continuar|prosseguir|desbloquear|receber|ativar|usar|utilizar|libertar|liberar|retirar|sacar|confirmar|regularizar|manter|concluir)\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly Regex OfficialAppContext = new Regex(
        @"\b(?:aplicativo|aplica[cç][aã]o|app|site|portal) oficial\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly Rule[] Rules =
    {
        MakeRule("credential-request", "Pedido de informação sensível", 5,
            @"\b(?:senha|palavra[- ]?passe|password|c[oó]digo(?: de (?:autentica[cç][aã]o|verifica[cç][aã]o|seguran[cç]a|acesso)| otp| sms)?|otp|pin|cvv|dados banc[aá]rios|dados pessoais|seus dados|n[uú]mero do cart[aã]o|documento de identidade|cpf|nif|passaporte)\b",
            "A mensagem parece solicitar um segredo, código ou dado pessoal; informar que um código existe não basta para ativar esta regra."),
        MakeRule("threat-pressure", "Ameaça ou pressão para agir", 3,
            @"\b(?:conta|cart[aã]o|acesso|servi[cç]o).{0,45}\b(?:bloquead[ao]|suspens[ao]|cancelad[ao])\b|\b(?:bloquead[ao]|suspens[ao]|cancelad[ao]).{0,45}\b(?:conta|cart[aã]o|acesso|servi[cç]o)\b|\b(?:perder[aá]|perder[aã]o|ser[aá] cancelad[ao]|ser[aá] bloquead[ao]|multa|pris[aã]o)\b",
            "Alega consequências relevantes para pressionar o destinatário."),
        MakeRule("immediate-pressure", "Prazo imediato ou urgência coerciva", 2,
            @"\b(?:imediatamente|agora mesmo|nos pr[oó]ximos minutos|em \d+ minutos|ultimas horas|[uú]ltima oportunidade|n[aã]o desligue|n[aã]o consulte|n[aã]o contacte|sem verificar|sem confirmar)\b",
            "A mensagem tenta impedir verificações independentes ou impõe um prazo excepcionalmente curto."),
        MakeRule("payment-request", "Pedido de pagamento ou transferência", 3,
            @"\b(?:pague|pagar|paga|transfira|fa[cç]a (?:(?:um|uma) )?(?:pix|pagamento|dep[oó]sito|transfer[eê]ncia)|realize (?:o )?pagamento|efetue (?:o )?pagamento|envie (?:o )?dinheiro|manda (?:o )?dinheiro|taxa (?:de )?(?:liberta[cç][aã]o|entrega|envio|alfand[eê]ga|pendente)|taxa para liberar|(?:d[áa](?:-me|\s+me)|dame|manda(?:-me)?|envia(?:-me)?|send\s+me|give\s+me)\s+(?:(?:[$€£]\s*)?\d+(?:[.,]\d{1,2})?(?:\s*(?:[$€£]|\b(?:euros?|d[oó]lares?|usd|eur|gbp|reais?|brl)\b))?|\b(?:dinheiro|money|plata)\b))(?=\W|$)|" + PaymentActionAndAmountPattern,
            "Pede uma transferência, pagamento ou taxa; o valor mencionado, isoladamente, não é contado como fraude."),
        MakeRule("reward-promise", "Promessa de prémio ou recompensa", 2,
            @"\b(?:ganhou|ganhaste|foi selecionad[ao]|foi contemplad[ao]|resgate (?:o )?pr[eê]mio|reclame (?:o )?pr[eê]mio|dinheiro gr[aá]tis|reembolso inesperado)\b",
            "Promete um benefício inesperado; o risco aumenta se a mensagem também pedir pagamento ou dados."),
        MakeRule("delivery-claim", "Alegação de encomenda retida ou entrega pendente", 2,
            @"\b(?:encomenda|pedido|pacote|entrega).{0,50}\b(?:retid[ao]|pendente|bloquead[ao]|n[aã]o entregue|taxa|libertar|liberar)\b|\b(?:retid[ao]|pendente).{0,40}\b(?:encomenda|pacote|entrega)\b",
            "Alega um problema de entrega; por si só, esta alegação não prova fraude."),
        MakeRule("authority-claim", "Alega representar uma entidade", 1,
            @"\b(?:somos|falamos em nome de|representamos|aqui [ée] do|[ée] do setor de seguran[cç]a d[oa])\b.{0,45}\b(?:banco|santander|millennium|cgd|paypal|ctt|dhl|correios|pol[ií]cia|finan[cç]as|receita federal|microsoft|apple|amazon|nubank)\b|\b(?:receita federal|finan[cç]as|banco|paypal|ctt|correios|nubank).{0,50}\b(?:bloqueou|suspendeu|cancelou|detectou|identificou)\b",
            "A alegação de identidade não foi verificada e só conta como contexto quando acompanhada por um pedido suspeito.")
    };

    public static ScamAnalysisResult Analyze(string original)
    {
        string message = original ?? "";
        if (message.Length > MaximumMessageLength)
        {
            throw new ArgumentException("A mensagem excede o limite de 20 000 caracteres.", "original");
        }

        PreparedMessage prepared = new PreparedMessage(message);
        ScamAnalysisResult result = new ScamAnalysisResult();
        result.Limitations.Add("A análise é local e baseada em regras; não confirma a identidade do remetente nem verifica o conteúdo de um site.");
        result.Limitations.Add("Uma mensagem sem sinais detetados ainda pode ser fraudulenta. Confirme pedidos importantes num canal oficial.");

        if (prepared.Normalized.Length < 12)
        {
            result.RiskLevel = ScamRiskLevel.Inconclusive;
            result.IsConclusive = false;
            result.Summary = "Há pouco contexto para avaliar esta mensagem com confiança.";
            result.Recommendations.Add("Se possível, analise a mensagem completa, incluindo o pedido e o contexto, sem acrescentar palavras-passe ou códigos.");
            return result;
        }

        foreach (Rule rule in Rules)
        {
            AddRuleEvidence(prepared, result, rule);
        }

        AddUrlEvidence(prepared, result);
        AddContextCombinations(prepared, result);
        FinalizeResult(prepared, result);
        return result;
    }

    private static void AddRuleEvidence(PreparedMessage prepared, ScamAnalysisResult result, Rule rule)
    {
        string[] sentences = SentenceSplit.Split(prepared.Normalized);
        int normalizedCursor = 0;
        foreach (string sentence in sentences)
        {
            int sentenceStart = prepared.Normalized.IndexOf(sentence, normalizedCursor, StringComparison.Ordinal);
            if (sentenceStart < 0)
            {
                sentenceStart = normalizedCursor;
            }
            normalizedCursor = Math.Min(prepared.Normalized.Length, sentenceStart + sentence.Length + 1);

            bool ruleMatches = rule.Expression.IsMatch(sentence) ||
                (rule.Id == "payment-request" && PaymentActionAndAmount.IsMatch(sentence));
            if (sentence.Length == 0 || !ruleMatches)
            {
                continue;
            }

            bool needsAction = rule.Id == "credential-request"
                ? RequestVerb.IsMatch(sentence)
                : rule.Id == "authority-claim"
                    ? true
                    : rule.Id == "payment-request" || rule.Id == "reward-promise" ||
                    rule.Id == "delivery-claim" || rule.Id == "threat-pressure" ||
                    rule.Id == "immediate-pressure";
            if (!needsAction)
            {
                continue;
            }

            Match awareness = NegationOrWarning.Match(sentence);
            if (awareness.Success)
            {
                Regex actionExpression = rule.Id == "credential-request" || rule.Id == "authority-claim"
                    ? RequestVerb
                    : rule.Expression;
                Match actionAfterWarning = actionExpression.Matches(sentence).Cast<Match>()
                    .FirstOrDefault(actionMatch => actionMatch.Index >= awareness.Index + awareness.Length);
                bool contradictoryAction = actionAfterWarning != null &&
                    ContrastMarker.Matches(sentence).Cast<Match>().Any(contrast =>
                        contrast.Index >= awareness.Index + awareness.Length &&
                        contrast.Index < actionAfterWarning.Index);
                if (ReportedMessageContext.IsMatch(sentence) || !contradictoryAction)
                {
                    continue;
                }
            }

            if (rule.Id == "payment-request" && OfficialAppContext.IsMatch(sentence) &&
                !prepared.Entities.Any(entity => entity.Kind == "url" && IsUnknownHost(entity.Host)))
            {
                continue;
            }

            Match match = rule.Expression.Match(sentence);
            string excerpt = prepared.GetOriginalExcerpt(sentenceStart, sentence.Length);
            AddEvidence(result, rule.Id, rule.Label, rule.Weight, excerpt, rule.Explanation);
            if (rule.Id == "payment-request" && PaymentActionAndAmount.IsMatch(sentence) &&
                PaymentCondition.IsMatch(sentence))
            {
                AddEvidence(result, "payment-condition-combination",
                    "Pagamento condicionado a uma ação", 2, excerpt,
                    "A mensagem associa um pedido de pagamento e um montante a uma condição para continuar, desbloquear, receber ou ativar algo. Isto merece verificação, mas não prova por si só que seja uma burla.");
            }

            if (rule.Id == "credential-request")
            {
                string target = match.Value;
                if (Regex.IsMatch(target, @"\b(?:c[oó]digo|otp|pin|cvv)\b",
                    RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
                {
                    AnalysisSignal signal = result.Signals.First(item => item.Id == rule.Id);
                    signal.Label = "Pedido de código de autenticação, PIN ou dado secreto";
                }
            }
        }
    }

    private static void AddUrlEvidence(PreparedMessage prepared, ScamAnalysisResult result)
    {
        foreach (MessageEntity entity in prepared.Entities.Where(item => item.Kind == "url"))
        {
            AddEvidence(result, "unverified-link", "Destino do link não verificado", 0, entity.Value,
                "O endereço foi analisado localmente, mas não foi aberto nem consultado. Confirme o domínio por um canal independente.");
            if (entity.IsShortener)
            {
                AddEvidence(result, "shortener-link", "URL encurtado", 2, entity.Value,
                    "O endereço encurtado esconde o domínio de destino; não foi aberto nem consultado.");
            }
            if (entity.IsIpAddress)
            {
                AddEvidence(result, "ip-link", "Link dirigido a endereço IP", 3, entity.Value,
                    "Um endereço IP em vez de um domínio é incomum em comunicações legítimas de consumo.");
            }
            if (entity.IsInternationalized || entity.IsLookalike)
            {
                AddEvidence(result, "lookalike-link", "Domínio potencialmente enganador", 4, entity.Value,
                    "O domínio usa caracteres internacionalizados ou imita visualmente o nome de uma marca conhecida.");
            }
            if (HasUnusualStructure(entity.Host))
            {
                AddEvidence(result, "unusual-domain", "Estrutura de domínio invulgar", 2, entity.Value,
                    "O endereço contém vários níveis ou uma estrutura difícil de atribuir ao serviço mencionado.");
            }
        }
    }

    private static void AddContextCombinations(PreparedMessage prepared, ScamAnalysisResult result)
    {
        AnalysisSignal payment = result.Signals.FirstOrDefault(signal => signal.Id == "payment-request");
        AnalysisSignal credential = result.Signals.FirstOrDefault(signal => signal.Id == "credential-request");
        AnalysisSignal reward = result.Signals.FirstOrDefault(signal => signal.Id == "reward-promise");
        AnalysisSignal delivery = result.Signals.FirstOrDefault(signal => signal.Id == "delivery-claim");
        AnalysisSignal urgency = result.Signals.FirstOrDefault(signal => signal.Id == "threat-pressure" ||
            signal.Id == "immediate-pressure");
        bool suspiciousUrl = result.Signals.Any(signal => signal.Id == "shortener-link" ||
            signal.Id == "ip-link" || signal.Id == "lookalike-link" || signal.Id == "unusual-domain");
        bool unrelatedOfficialApp = prepared.Normalized.Contains("aplicativo oficial") ||
            prepared.Normalized.Contains("app oficial") || prepared.Normalized.Contains("site oficial");

        if (payment != null && suspiciousUrl)
        {
            AddEvidence(result, "payment-link-combination", "Pagamento associado a um link suspeito", 3,
                payment.Evidence[0].Excerpt, "O pedido de pagamento surge junto de um URL com características suspeitas.");
        }
        if (credential != null && (urgency != null || suspiciousUrl))
        {
            AddEvidence(result, "credential-pressure-combination", "Pedido de dados combinado com pressão ou link suspeito", 3,
                credential.Evidence[0].Excerpt, "Pedir dados secretos e pressionar ou direcionar para um link aumenta o risco.");
        }
        if (reward != null && payment != null)
        {
            AddEvidence(result, "reward-payment-combination", "Pagamento exigido para reclamar uma recompensa", 3,
                reward.Evidence[0].Excerpt, "A mensagem promete uma recompensa mas exige pagamento para a receber.");
        }
        if (delivery != null && payment != null)
        {
            AddEvidence(result, "delivery-fee-combination", "Taxa exigida para libertar uma entrega", 2,
                delivery.Evidence[0].Excerpt, "A alegação de entrega pendente está associada a um pedido de pagamento.");
        }
        if (payment != null && urgency != null)
        {
            AddEvidence(result, "payment-pressure-combination", "Pagamento associado a pressão", 2,
                payment.Evidence[0].Excerpt, "O pedido de dinheiro é acompanhado por pressão para agir rapidamente ou evitar uma verificação.");
        }
        if (payment != null && Regex.IsMatch(prepared.Normalized,
            @"\b(?:nova conta|conta desconhecida|outra conta|n[aã]o consulte|n[aã]o confirme|n[aã]o contacte|n[aã]o contate|sem verificar)\b",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            AddEvidence(result, "unverified-transfer", "Transferência sem verificação independente", 3,
                payment.Evidence[0].Excerpt, "O pagamento é dirigido a uma conta nova ou acompanhado por instruções para não confirmar.");
        }
        if (result.Signals.Any(signal => signal.Id == "authority-claim") && credential != null &&
            !unrelatedOfficialApp)
        {
            AddEvidence(result, "authority-sensitive-request", "Entidade alegada pede dados sensíveis", 2,
                credential.Evidence[0].Excerpt, "Uma entidade alegada solicita dados que não devem ser partilhados por mensagem.");
        }

        if (prepared.Entities.Any(entity => entity.Kind == "url") &&
            result.Signals.Any(signal => signal.Id == "payment-request" || signal.Id == "credential-request") &&
            !suspiciousUrl)
        {
            AddEvidence(result, "action-link", "Pedido de ação acompanhado por link", 1,
                result.Signals.First(signal => signal.Id == "payment-request" ||
                    signal.Id == "credential-request").Evidence[0].Excerpt,
                "A mensagem associa um pedido de pagamento ou dados a um link; confirme o domínio por um canal independente.");
        }
    }

    private static void FinalizeResult(PreparedMessage prepared, ScamAnalysisResult result)
    {
        int directSignalScore = result.Signals
            .Where(signal => !IsContextCombination(signal.Id))
            .Sum(signal => signal.Weight);
        int strongestCombination = result.Signals
            .Where(signal => IsContextCombination(signal.Id))
            .Select(signal => signal.Weight)
            .DefaultIfEmpty(0)
            .Max();
        result.Score = directSignalScore + strongestCombination;
        bool hasActionableRisk = result.Signals.Any(signal =>
            signal.Id == "credential-request" || signal.Id == "payment-request" ||
            signal.Id == "immediate-pressure");
        bool hasSensitiveCombination = result.Signals.Any(signal =>
            signal.Id == "credential-pressure-combination" ||
            signal.Id == "payment-link-combination" ||
            signal.Id == "payment-pressure-combination" ||
            signal.Id == "payment-condition-combination" ||
            signal.Id == "unverified-transfer" ||
            signal.Id == "reward-payment-combination" ||
            signal.Id == "delivery-fee-combination" ||
            signal.Id == "authority-sensitive-request");

        if (result.Signals.Count == 0)
        {
            if (prepared.Normalized.Length < 30 ||
                prepared.Entities.Any(entity => entity.Kind == "url"))
            {
                result.RiskLevel = ScamRiskLevel.Inconclusive;
                result.IsConclusive = false;
                result.Summary = "Não foram encontrados sinais claros, mas o texto é demasiado curto para avaliar o contexto.";
                result.Recommendations.Add("Confirme pedidos inesperados por um contacto conhecido, sem usar números ou links incluídos na mensagem.");
            }
            else
            {
                result.RiskLevel = ScamRiskLevel.Low;
                result.IsConclusive = true;
                result.Summary = "Não foram encontrados sinais comuns de risco neste texto. Isto não garante que seja legítimo.";
                result.Recommendations.Add("Confirme por um canal oficial qualquer pedido de pagamento ou de dados pessoais.");
            }
        }
        else if ((hasSensitiveCombination && result.Score >= 8) ||
            (result.Signals.Any(signal => signal.Id == "credential-request") &&
                (hasSensitiveCombination || result.Signals.Any(signal => signal.Id == "lookalike-link" ||
                    signal.Id == "ip-link"))))
        {
            result.RiskLevel = ScamRiskLevel.High;
            result.IsConclusive = true;
            result.Summary = "Foram encontrados vários sinais independentes, incluindo um pedido acionável combinado com pressão, identidade alegada, pagamento ou um link suspeito.";
            result.Recommendations.Add("Não envie dinheiro, códigos, palavras-passe nem documentos através desta mensagem.");
            result.Recommendations.Add("Confirme a situação pela aplicação oficial ou por um número que já conheça, digitado manualmente.");
        }
        else if (hasActionableRisk || result.Score >= 4)
        {
            result.RiskLevel = ScamRiskLevel.Moderate;
            result.IsConclusive = true;
            result.Summary = "Há pelo menos um pedido ou sinal que merece verificação. Os elementos disponíveis não provam, por si só, que seja uma burla.";
            result.Recommendations.Add("Não cumpra o pedido antes de confirmar a identidade e o motivo por um canal independente.");
            result.Recommendations.Add("Não use links nem contactos fornecidos na própria mensagem para fazer essa confirmação.");
        }
        else
        {
            result.RiskLevel = ScamRiskLevel.Inconclusive;
            result.IsConclusive = false;
            result.Summary = "Foram encontrados elementos que merecem atenção, mas não há contexto suficiente para classificar o risco.";
            result.Recommendations.Add("Verifique quem enviou a mensagem e procure o contexto completo antes de agir.");
        }

        bool containsMoney = prepared.Entities.Any(entity => entity.Kind == "payment-reference") ||
            Regex.IsMatch(prepared.Normalized, @"\b(?:\d+\s?(?:euros?|reais?|d[oó]lares?|eur|brl|usd)|pix|iban|mb way)\b",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        if (containsMoney && !result.Signals.Any(signal => signal.Id == "payment-request"))
        {
            result.Limitations.Add("Valores e referências de pagamento foram encontrados, mas não são tratados como prova de pedido fraudulento.");
        }
    }

    private static void AddEvidence(
        ScamAnalysisResult result, string id, string label, int weight, string excerpt, string explanation)
    {
        AnalysisSignal signal = result.Signals.FirstOrDefault(item => item.Id == id);
        if (signal == null)
        {
            signal = new AnalysisSignal { Id = id, Label = label, Weight = weight };
            result.Signals.Add(signal);
        }
        else if (weight > signal.Weight)
        {
            signal.Weight = weight;
        }

        if (!signal.Evidence.Any(item => item.Excerpt == excerpt) && signal.Evidence.Count < 3)
        {
            signal.Evidence.Add(new AnalysisEvidence
            {
                Excerpt = excerpt.Length <= 240 ? excerpt : excerpt.Substring(0, 237) + "...",
                Explanation = explanation
            });
        }
    }

    private static bool IsUnknownHost(string host)
    {
        return !String.IsNullOrEmpty(host) &&
            (host.IndexOf("xn--", StringComparison.OrdinalIgnoreCase) >= 0 ||
             host.Split('.').Length > 4);
    }

    private static bool IsContextCombination(string id)
    {
        return id == "payment-link-combination" ||
            id == "credential-pressure-combination" ||
            id == "reward-payment-combination" ||
            id == "delivery-fee-combination" ||
            id == "authority-sensitive-request" ||
            id == "payment-pressure-combination" ||
            id == "payment-condition-combination" ||
            id == "unverified-transfer" ||
            id == "action-link";
    }

    private static bool HasUnusualStructure(string host)
    {
        return !String.IsNullOrEmpty(host) &&
            host.Split('.').Length >= 5;
    }

    private static Rule MakeRule(string id, string label, int weight, string expression, string explanation)
    {
        return new Rule
        {
            Id = id,
            Label = label,
            Weight = weight,
            Explanation = explanation,
            Expression = new Regex(expression, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)
        };
    }
}
