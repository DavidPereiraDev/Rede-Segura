using System;
using System.Collections.Generic;
using System.Linq;

internal sealed class ScamAnalysisTestCase
{
    public string Name;
    public string Message;
    public ScamRiskLevel Expected;
}

internal static class ScamAnalyzerTests
{
    private static ScamAnalysisTestCase Case(string name, string message, ScamRiskLevel expected)
    {
        return new ScamAnalysisTestCase { Name = name, Message = message, Expected = expected };
    }

    private static readonly ScamAnalysisTestCase[] Cases =
    {
        Case("PT bank password link", "O Banco Santander pede que envie a sua palavra-passe agora para evitar o bloqueio da conta. https://bit.ly/conta", ScamRiskLevel.High),
        Case("PT bank OTP urgency", "A sua conta foi bloqueada. Envie o código de autenticação imediatamente para recuperar o acesso.", ScamRiskLevel.High),
        Case("PT CTT delivery fee", "A sua encomenda está retida. Pague uma taxa de entrega em https://tinyurl.com/ctt-pagamento", ScamRiskLevel.High),
        Case("PT prize fee", "Ganhou um prémio exclusivo! Pague uma taxa de envio para receber o prémio.", ScamRiskLevel.High),
        Case("PT fake invoice shortener", "A fatura está vencida. Pague agora através deste link https://t.co/fatura para evitar o cancelamento.", ScamRiskLevel.High),
        Case("PT fake bank IP link", "Somos do banco e precisamos do seu PIN para desbloquear a conta. Aceda a http://192.0.2.10/seguranca", ScamRiskLevel.High),
        Case("PT password urgency", "A sua palavra-passe foi comprometida. Envie a senha agora mesmo para evitar perder a conta.", ScamRiskLevel.High),
        Case("PT courier customs tax", "A encomenda está retida na alfândega. Pague a taxa de libertação em https://entrega.example.pt/pagar", ScamRiskLevel.Moderate),
        Case("PT Pix account scam", "Sua conta será bloqueada. Envie sua senha e o código OTP imediatamente.", ScamRiskLevel.High),
        Case("PT fake authority CPF", "Falamos em nome da Receita Federal. Envie seu CPF e senha agora para evitar multa.", ScamRiskLevel.High),
        Case("PT prize asks bank data", "Você foi selecionado e ganhou um prêmio. Informe seus dados bancários para receber o dinheiro grátis.", ScamRiskLevel.Moderate),
        Case("PT delivery urgent link", "O seu pacote está retido e será devolvido. Pague a taxa nos próximos minutos: http://203.0.113.9/pix", ScamRiskLevel.High),
        Case("PT lookalike Microsoft", "A sua conta Microsoft foi suspensa. Digite a sua senha e o código de segurança em https://micr0soft.example.com/login", ScamRiskLevel.High),
        Case("PT PayPal suspicious link", "Somos do PayPal. Envie o código de acesso agora para desbloquear a conta: https://paypa1.example.com/confirmar", ScamRiskLevel.High),
        Case("PT transfer to unknown account", "Transfira 350 euros para esta nova conta imediatamente e não confirme com ninguém.", ScamRiskLevel.High),
        Case("PT avoid verification", "Pague 40 euros agora e não contacte o banco para verificar esta operação.", ScamRiskLevel.High),
        Case("PT SMS asks CVV", "Envie o CVV do seu cartão para confirmar a entrega da encomenda.", ScamRiskLevel.Moderate),
        Case("PT SMS requests NIF", "Informe o seu NIF e documento de identidade para atualizar já o cadastro.", ScamRiskLevel.Moderate),
        Case("PT fake bank representative", "Aqui é do setor de segurança do banco. Diga o seu PIN e o código de autenticação agora.", ScamRiskLevel.High),
        Case("PT informal money request", "Dame 50 $ agora, por favor.", ScamRiskLevel.Moderate),
        Case("PT Brazilian informal Pix", "Me manda 80 reais por Pix agora, por favor.", ScamRiskLevel.Moderate),
        Case("PT password only", "Envie a palavra-passe para esta conta.", ScamRiskLevel.Moderate),
        Case("PT warning contradicted by direct request", "O banco nunca pede códigos de acesso por mensagem, mas envie-me o código para verificar a conta.", ScamRiskLevel.Moderate),
        Case("PT account wording is not official context", "A sua conta será bloqueada. Pague a taxa na sua conta hoje.", ScamRiskLevel.High),
        Case("PT conditional payment dollar suffix", "para continuar envie 43$", ScamRiskLevel.Moderate),
        Case("PT conditional payment dollar prefix", "Envie $43 para continuar.", ScamRiskLevel.Moderate),
        Case("PT conditional payment USD", "Manda 43 USD para desbloquear a conta.", ScamRiskLevel.Moderate),
        Case("PT conditional payment euro suffix", "Transfira 43€ para receber o documento.", ScamRiskLevel.Moderate),
        Case("PT conditional payment decimal euros", "Pague 1,95 € para ativar o serviço.", ScamRiskLevel.Moderate),
        Case("PT conditional payment deposit EUR", "Deposite 43 EUR para libertar a encomenda.", ScamRiskLevel.Moderate),
        Case("PT app payment mixed-case gerund", "continue enviAnDo 45$ para usar o nosso app", ScamRiskLevel.Moderate),
        Case("PT conditional payment infinitive USD", "Enviar 45 USD para continuar a usar a aplicação.", ScamRiskLevel.Moderate),
        Case("PT conditional payment prefix amount", "Envie $45 para desbloquear a conta.", ScamRiskLevel.Moderate),
        Case("PT conditional payment gerund euros", "Pagando 45€ para ativar o serviço.", ScamRiskLevel.Moderate),
        Case("PT conditional payment transfer typo", "Transferrindo 45 EUR para receber acesso.", ScamRiskLevel.Moderate),
        Case("PT conditional payment verb typo", "Continue envinando 45 euros para usar o app.", ScamRiskLevel.Moderate),
        Case("PT legitimate conditional invoice", "A fatura de 43 EUR está disponível. Pague pela aplicação oficial para continuar.", ScamRiskLevel.Low),
        Case("PT legitimate conditional subscription", "Pague 1,95 € pela aplicação oficial para manter a subscrição ativa.", ScamRiskLevel.Low),
        Case("PT legitimate service payment", "Pague 12 EUR pela mensalidade do serviço através da aplicação oficial para continuar.", ScamRiskLevel.Low),
        Case("PT legitimate purchase", "Pague 45 euros pela compra através do site oficial.", ScamRiskLevel.Low),
        Case("PT amount without payment request", "O saldo atual é 43$ e a fatura de 1,95 € foi liquidada.", ScamRiskLevel.Low),
        Case("PT past payment is not a request", "Enviei 43$ para prosseguir com a compra.", ScamRiskLevel.Low),
        Case("PT suspicious unknown link", "Clique aqui para consultar os detalhes da sua conta: https://secure-login.example.xyz/validar", ScamRiskLevel.Inconclusive),
        Case("PT short message", "Olá, tudo bem?", ScamRiskLevel.Inconclusive),
        Case("PT empty after spaces", "     ", ScamRiskLevel.Inconclusive),

        Case("BR fake bank access", "Seu acesso será suspenso. Digite sua senha e o código OTP neste link agora: https://bit.ly/acesso", ScamRiskLevel.High),
        Case("BR boleto urgency", "Seu boleto venceu. Pague imediatamente para evitar multa e bloqueio da conta.", ScamRiskLevel.High),
        Case("BR fake Correios fee", "Seu pacote está retido. Pague uma taxa para liberar a entrega em https://tinyurl.com/correios", ScamRiskLevel.High),
        Case("BR Pix reward scam", "Você ganhou um prêmio! Faça um Pix de 20 reais para liberar o saque.", ScamRiskLevel.High),
        Case("BR fake Nubank support", "Somos do Nubank. Envie seu código de segurança para cancelar uma compra agora.", ScamRiskLevel.High),
        Case("BR CPF and password", "Informe seu CPF e sua senha imediatamente para evitar o cancelamento do benefício.", ScamRiskLevel.High),
        Case("BR fake Receita link", "A Receita Federal bloqueou seu cadastro. Acesse http://198.51.100.5/regularizar e envie seus dados.", ScamRiskLevel.High),
        Case("BR suspicious domain unicode", "Atualize sua conta e digite sua senha em https://xn--pple-43d.example/seguranca", ScamRiskLevel.High),
        Case("BR request transfer", "Faça uma transferência de 500 reais para esta conta nova sem avisar sua família.", ScamRiskLevel.Moderate),
        Case("BR package fee no link", "Seu pedido está pendente. Pague a taxa de entrega para receber o pacote.", ScamRiskLevel.Moderate),
        Case("BR gift card prize", "Você foi sorteado. Pague uma taxa de resgate para receber seu prêmio.", ScamRiskLevel.Moderate),
        Case("BR WhatsApp OTP", "Me envie o código de autenticação do WhatsApp agora para eu confirmar sua conta.", ScamRiskLevel.Moderate),
        Case("BR fake streaming", "Sua conta será cancelada hoje. Digite a senha e os dados do cartão neste link https://bit.ly/stream", ScamRiskLevel.High),
        Case("BR no verification pressure", "Transfira o valor agora mesmo e não consulte outra pessoa.", ScamRiskLevel.High),
        Case("BR school scholarship", "A escola publicou o resultado da bolsa de estudos no aplicativo oficial.", ScamRiskLevel.Low),
        Case("BR ordinary chat", "Oi, vou passar na sua casa às seis e depois vamos jantar com a família.", ScamRiskLevel.Low),
        Case("BR appointment", "Sua consulta está marcada para terça-feira às dez. Traga o documento de identidade.", ScamRiskLevel.Low),
        Case("BR work meeting", "A reunião de trabalho foi marcada para amanhã às nove. Confirme presença no calendário.", ScamRiskLevel.Low),
        Case("BR grocery cost", "Comprei pão, leite e café por 50 reais no mercado hoje de manhã.", ScamRiskLevel.Low),
        Case("BR ordinary payment receipt", "O pagamento de 80 reais foi recebido e o recibo está disponível no aplicativo oficial.", ScamRiskLevel.Low),
        Case("BR legitimate invoice app", "A fatura de 80 euros vence em 30 dias. Consulte e pague pela aplicação oficial.", ScamRiskLevel.Low),
        Case("BR school homework", "A professora pediu um trabalho sobre segurança digital para entregar na próxima semana.", ScamRiskLevel.Low),
        Case("BR informative bank notice", "O banco informa que o extrato mensal está disponível na aplicação oficial.", ScamRiskLevel.Low),
        Case("BR package tracking neutral", "A encomenda saiu do centro de distribuição e deve chegar na quinta-feira.", ScamRiskLevel.Low),
        Case("BR regular bill deadline", "A conta de água vence no dia 20 do próximo mês. O débito automático está ativo.", ScamRiskLevel.Low),
        Case("BR code warning", "Seu código é 123456. Não compartilhe esse código com ninguém.", ScamRiskLevel.Low),
        Case("BR bank password warning", "O banco nunca pede sua senha ou código por telefone, email ou mensagem.", ScamRiskLevel.Low),
        Case("BR educational scam warning", "Alerta de golpe: golpistas podem pedir códigos e dados bancários por mensagens falsas.", ScamRiskLevel.Low),
        Case("BR family warning", "Nunca envie Pix para alguém que diz ser familiar sem confirmar por chamada conhecida.", ScamRiskLevel.Low),
        Case("BR prize news", "A escola premiou os alunos que venceram a competição de matemática.", ScamRiskLevel.Low),
        Case("BR amount only", "O jantar para quatro pessoas custou 120 reais, incluindo sobremesa e bebidas.", ScamRiskLevel.Low),
        Case("BR contact details", "Guarde o telefone 912 345 678 e o email pessoa@example.com para falar amanhã.", ScamRiskLevel.Low),
        Case("PT no entity proof", "O Banco de Portugal publicou hoje um relatório sobre pagamentos digitais.", ScamRiskLevel.Low),
        Case("PT educational phishing", "Este aviso explica como identificar phishing e proteger palavras-passe e códigos.", ScamRiskLevel.Low),
        Case("PT official app invoice", "A fatura está disponível na aplicação oficial e pode consultá-la quando quiser.", ScamRiskLevel.Low),
        Case("PT term code not request", "O código de acesso à sala de aula está no portal da escola.", ScamRiskLevel.Low),
        Case("PT transfer confirmation", "A transferência de 25 euros foi concluída e o comprovativo está na aplicação oficial.", ScamRiskLevel.Low),
        Case("PT deadline normal", "A inscrição termina no dia 30 do próximo mês; consulte o calendário escolar.", ScamRiskLevel.Low),
        Case("PT gift conversation", "Comprei um presente de aniversário para a minha irmã e vou entregá-lo amanhã.", ScamRiskLevel.Low),
        Case("PT harmless link needs verification", "Veja o programa da festa da escola em https://escola.example.pt/programa.", ScamRiskLevel.Inconclusive),
        Case("PT contextual code denial", "O teu código é 123456. Não o partilhes com ninguém, nem comigo.", ScamRiskLevel.Low),
        Case("PT quote reports scam", "Recebi uma mensagem que dizia: \"envie a sua senha agora\". Não respondi e bloqueei o remetente.", ScamRiskLevel.Low),
        Case("PT warning plus request quote", "Aviso sobre burlas: nunca envie o código de autenticação a quem o pedir por mensagem.", ScamRiskLevel.Low),
        Case("PT enrollment payment", "Pague a inscrição de 25 euros pela aplicação oficial da escola até ao fim do mês.", ScamRiskLevel.Low),
        Case("PT email from teacher", "A professora enviou o horário e pediu para confirmar a presença na visita de estudo.", ScamRiskLevel.Low),
        Case("PT payment deadline standard", "Pague a mensalidade até 30 de novembro no portal oficial da universidade.", ScamRiskLevel.Low),
        Case("PT inconclusive mixed signals", "A conta foi bloqueada, mas não sei se a mensagem é real.", ScamRiskLevel.Inconclusive),
        Case("PT unknown domain many labels", "Consulte a página em https://login.conta.segura.exemplo.pt para ver informações.", ScamRiskLevel.Inconclusive),
        Case("BR scheduled payment notice", "O pagamento da mensalidade foi agendado para o dia 10 no aplicativo oficial.", ScamRiskLevel.Low),
        Case("BR official bank code notice", "Seu código de acesso foi criado. O banco nunca solicita esse código por mensagem.", ScamRiskLevel.Low),
        Case("BR package normal", "Seu pacote foi enviado e o rastreamento está disponível no site oficial da loja.", ScamRiskLevel.Low)
    };

    public static int Run()
    {
        int failures = 0;
        int falsePositiveCases = 0;
        int falseNegativeCases = 0;
        int ambiguousCases = 0;
        foreach (ScamAnalysisTestCase testCase in Cases)
        {
            try
            {
                ScamAnalysisResult actual = ScamAnalyzer.Analyze(testCase.Message);
                if (testCase.Expected == ScamRiskLevel.Inconclusive)
                {
                    ambiguousCases++;
                }
                if (testCase.Expected == ScamRiskLevel.Low && actual.RiskLevel != ScamRiskLevel.Low)
                {
                    falsePositiveCases++;
                }
                if ((testCase.Expected == ScamRiskLevel.Moderate || testCase.Expected == ScamRiskLevel.High) &&
                    (actual.RiskLevel == ScamRiskLevel.Low || actual.RiskLevel == ScamRiskLevel.Inconclusive))
                {
                    falseNegativeCases++;
                }
                if (actual.RiskLevel != testCase.Expected)
                {
                    failures++;
                    Console.Error.WriteLine(
                        "FALHOU [{0}] esperado={1}, real={2}, sinais={3}",
                        testCase.Name,
                        testCase.Expected,
                        actual.RiskLevel,
                        String.Join(", ", actual.Signals.Select(signal => signal.Id).ToArray()));
                }
                else
                {
                    Console.WriteLine("PASS [{0}] esperado=real={1}", testCase.Name, actual.RiskLevel);
                }
            }
            catch (Exception error)
            {
                failures++;
                Console.Error.WriteLine("ERRO [{0}] {1}", testCase.Name, error.Message);
            }
        }

        bool evidenceIsConcrete = ScamAnalyzer.Analyze(
            "Envie o código de autenticação imediatamente para evitar o bloqueio da conta.")
            .Signals.Any(signal => signal.Evidence.Any(evidence =>
                evidence.Excerpt.IndexOf("código", StringComparison.OrdinalIgnoreCase) >= 0));
        if (!evidenceIsConcrete)
        {
            failures++;
            Console.Error.WriteLine("FALHOU [evidence] não foi devolvido um excerto original relevante.");
        }

        bool sizeLimitWorks = false;
        try
        {
            ScamAnalyzer.Analyze(new string('a', 20001));
        }
        catch (ArgumentException)
        {
            sizeLimitWorks = true;
        }
        if (!sizeLimitWorks)
        {
            failures++;
            Console.Error.WriteLine("FALHOU [input-limit] não foi aplicado o limite de entrada.");
        }
        if (ScamAnalyzer.Analyze(new string('a', ScamAnalyzer.MaximumMessageLength)).RiskLevel != ScamRiskLevel.Low)
        {
            failures++;
            Console.Error.WriteLine("FALHOU [maximum-input] uma mensagem no limite permitido não foi aceite.");
        }

        PreparedMessage prepared = new PreparedMessage(
            "   DÁ-ME 50 $ agora. Contacto: pessoa@example.com, 912 345 678. https://paypa1.example.com/pagar");
        if (!prepared.Normalized.StartsWith("da-me 50 $ agora", StringComparison.Ordinal) ||
            !prepared.Entities.Any(entity => entity.Kind == "email") ||
            !prepared.Entities.Any(entity => entity.Kind == "phone") ||
            !prepared.Entities.Any(entity => entity.Kind == "url" && entity.IsLookalike))
        {
            failures++;
            Console.Error.WriteLine("FALHOU [normalization-entities] Unicode ou entidades locais não foram reconhecidos.");
        }

        ScamAnalysisResult negativeExample = ScamAnalyzer.Analyze(
            "O banco nunca pede a sua senha nem o código de autenticação. Este é um aviso educativo.");
        if (negativeExample.RiskLevel != ScamRiskLevel.Low || negativeExample.Signals.Count != 0)
        {
            failures++;
            Console.Error.WriteLine("FALHOU [negation] o aviso educativo foi classificado como pedido.");
        }

        ScamAnalysisResult disguisedRequest = ScamAnalyzer.Analyze(
            "A sua conta foi bloqueada por fraude. Envie o código de autenticação para a desbloquear agora.");
        if (!disguisedRequest.Signals.Any(signal => signal.Id == "credential-request"))
        {
            failures++;
            Console.Error.WriteLine("FALHOU [context-evasion] menção a fraude escondeu um pedido de código.");
        }

        ScamAnalysisResult conditionalPayment = ScamAnalyzer.Analyze("para continuar envie 43$");
        if (conditionalPayment.RiskLevel != ScamRiskLevel.Moderate ||
            conditionalPayment.Score != 5 ||
            !conditionalPayment.Signals.Any(signal => signal.Id == "payment-request") ||
            !conditionalPayment.Signals.Any(signal =>
                signal.Id == "payment-condition-combination" &&
                signal.Evidence.Any(evidence => evidence.Excerpt == "para continuar envie 43$" &&
                    evidence.Explanation.IndexOf("não prova", StringComparison.OrdinalIgnoreCase) >= 0)))
        {
            failures++;
            Console.Error.WriteLine("FALHOU [conditional-payment] o montante, a condição e a explicação não foram associados.");
        }

        string[] conditionalPaymentVariants =
        {
            "continue enviAnDo 45$ para usar o nosso app",
            "Enviar 45 USD para continuar a usar a aplicação.",
            "Envie $45 para desbloquear a conta.",
            "Pagando 45€ para ativar o serviço.",
            "Transferrindo 45 EUR para receber acesso.",
            "Continue envinando 45 euros para usar o app."
        };
        foreach (string variant in conditionalPaymentVariants)
        {
            ScamAnalysisResult analysis = ScamAnalyzer.Analyze(variant);
            if (analysis.RiskLevel != ScamRiskLevel.Moderate ||
                analysis.Score <= 0 ||
                !analysis.Signals.Any(signal => signal.Id == "payment-request") ||
                !analysis.Signals.Any(signal =>
                    signal.Id == "payment-condition-combination" &&
                    signal.Evidence.Any(evidence =>
                        evidence.Explanation.IndexOf("não prova", StringComparison.OrdinalIgnoreCase) >= 0)) ||
                analysis.Summary.IndexOf("não provam", StringComparison.OrdinalIgnoreCase) < 0)
            {
                failures++;
                Console.Error.WriteLine(
                    "FALHOU [conditional-payment-variant] o pedido ou a incerteza não foram reconhecidos: {0}",
                    variant);
            }
        }

        Console.WriteLine(
            "{0} casos de mensagens; falsos positivos esperados/observados: {1}; falsos negativos esperados/observados: {2}; casos ambíguos: {3}; falhas: {4}.",
            Cases.Length, falsePositiveCases, falseNegativeCases, ambiguousCases, failures);
        return failures == 0 ? 0 : 1;
    }

    private static int Main()
    {
        return Run();
    }
}
