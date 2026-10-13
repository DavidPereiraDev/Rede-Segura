using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

internal sealed class SecurityContentCard : Panel
{
    private readonly TableLayoutPanel content;
    private readonly Label title;
    private readonly List<Label> sectionTitles = new List<Label>();
    private readonly List<Label> sectionBodies = new List<Label>();
    private bool darkMode = true;
    private int previousWidth;

    public SecurityContentCard(string cardTitle, string[][] sections)
    {
        BackColor = Color.FromArgb(32, 42, 48);
        Margin = new Padding(0, 0, 0, 10);
        Padding = new Padding(16, 14, 16, 12);
        Tag = "security-card";
        AccessibleRole = AccessibleRole.Grouping;

        content = new TableLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BackColor = Color.Transparent,
            ColumnCount = 1,
            Dock = DockStyle.Top,
            Margin = new Padding(0),
            Padding = new Padding(0)
        };
        content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        content.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        title = new Label
        {
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 13F, FontStyle.Bold),
            Margin = new Padding(0, 0, 0, 8),
            Tag = "security-card-title",
            Text = cardTitle
        };
        content.Controls.Add(title, 0, 0);

        int row = 1;
        foreach (string[] section in sections)
        {
            Label sectionTitle = new Label
            {
                AutoSize = true,
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                Margin = new Padding(0, 1, 0, 2),
                Tag = "security-card-heading",
                Text = section[0]
            };
            Label sectionBody = new Label
            {
                AutoSize = true,
                Font = new Font("Segoe UI", 9.5F),
                Margin = new Padding(0, 0, 0, 7),
                Tag = "security-card-body",
                Text = section[1]
            };
            content.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            content.Controls.Add(sectionTitle, 0, row++);
            content.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            content.Controls.Add(sectionBody, 0, row++);
            sectionTitles.Add(sectionTitle);
            sectionBodies.Add(sectionBody);
        }

        Controls.Add(content);
        SizeChanged += delegate
        {
            if (Width != previousWidth)
            {
                previousWidth = Width;
                UpdateCardLayout();
            }
        };
        ApplyTheme(true);
        UpdateCardLayout();
    }

    public void ApplyTheme(bool useDarkMode)
    {
        darkMode = useDarkMode;
        BackColor = darkMode ? Color.FromArgb(32, 42, 48) : Color.White;
        title.ForeColor = darkMode ? Color.FromArgb(239, 244, 241) : Color.FromArgb(35, 48, 42);
        foreach (Label sectionTitle in sectionTitles)
        {
            sectionTitle.ForeColor = darkMode
                ? Color.FromArgb(139, 221, 190)
                : Color.FromArgb(23, 107, 80);
        }
        foreach (Label sectionBody in sectionBodies)
        {
            sectionBody.ForeColor = darkMode
                ? Color.FromArgb(220, 231, 226)
                : Color.FromArgb(54, 68, 61);
        }
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        using (Pen border = new Pen(darkMode
            ? Color.FromArgb(57, 72, 78)
            : Color.FromArgb(224, 233, 226)))
        {
            Rectangle bounds = ClientRectangle;
            bounds.Width -= 1;
            bounds.Height -= 1;
            e.Graphics.DrawRectangle(border, bounds);
        }
    }

    private void UpdateCardLayout()
    {
        if (content == null)
        {
            return;
        }

        int contentWidth = Math.Max(180, ClientSize.Width - Padding.Horizontal);
        content.Width = contentWidth;
        foreach (Control child in content.Controls)
        {
            Label label = child as Label;
            if (label != null)
            {
                label.MaximumSize = new Size(contentWidth, 0);
            }
        }
        content.PerformLayout();
        Height = content.GetPreferredSize(new Size(contentWidth, 0)).Height +
            Padding.Vertical + 2;
    }
}

internal abstract class SecurityContentPage : UserControl
{
    private readonly Panel scrollViewport;
    private readonly FlowLayoutPanel cardList;
    private readonly List<SecurityContentCard> cards = new List<SecurityContentCard>();
    private bool darkMode = true;

    protected SecurityContentPage()
    {
        Dock = DockStyle.Fill;
        BackColor = Color.FromArgb(23, 31, 36);
        Margin = new Padding(0);
        Padding = new Padding(0);
        AccessibleRole = AccessibleRole.Pane;

        scrollViewport = new Panel
        {
            AutoScroll = true,
            BackColor = BackColor,
            Dock = DockStyle.Fill,
            Padding = new Padding(20),
            Tag = "security-page"
        };
        cardList = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BackColor = Color.Transparent,
            Dock = DockStyle.Top,
            FlowDirection = FlowDirection.TopDown,
            Margin = new Padding(0),
            Padding = new Padding(0),
            WrapContents = false,
            AccessibleRole = AccessibleRole.List
        };
        scrollViewport.Controls.Add(cardList);
        Controls.Add(scrollViewport);
        scrollViewport.SizeChanged += delegate { ResizeCards(); };
    }

    public void ApplyTheme(bool useDarkMode)
    {
        darkMode = useDarkMode;
        BackColor = darkMode ? Color.FromArgb(23, 31, 36) : Color.FromArgb(244, 247, 242);
        scrollViewport.BackColor = BackColor;
        foreach (SecurityContentCard card in cards)
        {
            card.ApplyTheme(darkMode);
        }
    }

    protected void AddCard(string title, params string[][] sections)
    {
        SecurityContentCard card = new SecurityContentCard(title, sections)
        {
            Width = Math.Max(220, cardList.ClientSize.Width),
            AccessibleName = title
        };
        cards.Add(card);
        cardList.Controls.Add(card);
        card.ApplyTheme(darkMode);
        ResizeCards();
    }

    private void ResizeCards()
    {
        if (scrollViewport == null || cardList == null)
        {
            return;
        }

        int width = Math.Max(220, scrollViewport.ClientSize.Width -
            SystemInformation.VerticalScrollBarWidth - scrollViewport.Padding.Horizontal);
        cardList.Width = width;
        foreach (SecurityContentCard card in cards)
        {
            card.Width = width;
        }
    }
}

internal sealed class LearningControl : SecurityContentPage
{
    public LearningControl()
    {
        AddCard("Phishing", new[]
        {
            new[] { "O que é", "É uma mensagem que tenta imitar uma entidade conhecida para levar a pessoa a revelar dados, abrir um anexo ou carregar num link." },
            new[] { "Sinais de alerta", "Urgência inesperada, endereço do remetente estranho, pedido de palavra-passe ou link diferente do endereço oficial." },
            new[] { "Exemplo fictício", "«A sua conta será encerrada hoje. Confirme os seus dados em https://acesso.exemplo.invalid.»" },
            new[] { "Como prevenir", "Não responda nem use o link da mensagem. Abra a aplicação ou escreva manualmente o endereço que já conhece." }
        });
        AddCard("Burlas por SMS", new[]
        {
            new[] { "O que é", "Uma SMS inesperada pode fingir ser de uma empresa ou pessoa conhecida e pedir uma ação rápida." },
            new[] { "Sinais de alerta", "Ameaças de bloqueio, pedidos de pagamento, códigos de autenticação ou links encurtados sem contexto." },
            new[] { "Exemplo fictício", "«O serviço será suspenso dentro de minutos. Pague a taxa neste link para evitar o bloqueio.»" },
            new[] { "Como prevenir", "Pare antes de responder. Confirme o pedido por um contacto ou aplicação oficial que já utilize." }
        });
        AddCard("Burlas bancárias", new[]
        {
            new[] { "O que é", "Alguém pode fazer-se passar por um banco para obter credenciais, códigos ou uma transferência." },
            new[] { "Sinais de alerta", "Pedido de PIN, palavra-passe ou código; pressão para transferir dinheiro; instruções para não contactar o banco." },
            new[] { "Exemplo fictício", "«Detetámos um problema. Diga-nos o código que recebeu para proteger a sua conta.»" },
            new[] { "Como prevenir", "Nunca partilhe códigos ou credenciais por mensagem. Termine o contacto e confirme através da aplicação oficial ou de um número conhecido." }
        });
        AddCard("Falsas entregas e encomendas", new[]
        {
            new[] { "O que é", "Uma mensagem pode inventar uma encomenda retida e pedir uma taxa ou dados para a libertar." },
            new[] { "Sinais de alerta", "Não reconhece a encomenda, existe um prazo muito curto ou o pagamento é pedido através de um link inesperado." },
            new[] { "Exemplo fictício", "«A encomenda está retida. Pague uma taxa de entrega para receber o pacote.»" },
            new[] { "Como prevenir", "Consulte o estado da encomenda na aplicação ou no site oficial da transportadora, sem usar o link recebido." }
        });
        AddCard("Falsificação de identidade", new[]
        {
            new[] { "O que é", "Uma pessoa pode usar o nome, a fotografia ou o endereço de alguém conhecido para parecer legítima." },
            new[] { "Sinais de alerta", "Pedido inesperado de dinheiro ou informação, mudança de conta de pagamento ou insistência em manter segredo." },
            new[] { "Exemplo fictício", "«Sou eu, troquei de número. Preciso que envies dinheiro para esta conta agora.»" },
            new[] { "Como prevenir", "Confirme a identidade por outro meio, por exemplo ligando para um número que já tinha guardado." }
        });
        AddCard("Pedidos de códigos de autenticação", new[]
        {
            new[] { "O que é", "Um código de utilização única pode permitir entrar numa conta ou confirmar uma operação." },
            new[] { "Sinais de alerta", "Alguém pede que leia, reencaminhe ou introduza um código que acabou de receber." },
            new[] { "Exemplo fictício", "«Para cancelar a operação, envie-nos o código de seis dígitos que recebeu.»" },
            new[] { "Como prevenir", "Não partilhe códigos. Se não iniciou a ação associada, ignore o pedido e aceda à conta através da aplicação oficial." }
        });
        AddCard("Falsos prémios", new[]
        {
            new[] { "O que é", "Uma mensagem promete um prémio inesperado para levar a pessoa a pagar uma taxa ou fornecer dados." },
            new[] { "Sinais de alerta", "Não participou num sorteio, existe pressão para reclamar já ou pedem pagamento para receber o prémio." },
            new[] { "Exemplo fictício", "«Foi selecionado para receber um prémio. Pague os custos de entrega ainda hoje.»" },
            new[] { "Como prevenir", "Não pague nem forneça dados para reclamar uma oferta inesperada. Confirme a campanha por canais oficiais." }
        });
        AddCard("Pagamentos fraudulentos", new[]
        {
            new[] { "O que é", "Um burlão pode pedir uma transferência, pagamento ou alteração de dados de pagamento usando uma história convincente." },
            new[] { "Sinais de alerta", "Conta bancária nova, pedido urgente, segredo ou instruções para evitar uma confirmação independente." },
            new[] { "Exemplo fictício", "«Pague esta fatura para a nova conta indicada e não contacte o fornecedor.»" },
            new[] { "Como prevenir", "Verifique o pedido e os dados de pagamento através de um contacto conhecido antes de transferir dinheiro." }
        });
        AddCard("Segurança de palavras-passe", new[]
        {
            new[] { "O que é", "Uma palavra-passe longa e exclusiva reduz o impacto de uma fuga num serviço sobre as restantes contas." },
            new[] { "Sinais de alerta", "A mesma palavra-passe é reutilizada, é fácil de adivinhar ou alguém pede que a envie por mensagem." },
            new[] { "Exemplo fictício", "Uma frase-passe longa e diferente para cada conta é mais segura do que repetir a mesma palavra-passe." },
            new[] { "Como prevenir", "Use palavras-passe exclusivas, considere um gestor de palavras-passe e ative autenticação adicional quando disponível." }
        });
        AddCard("Reconhecer links suspeitos", new[]
        {
            new[] { "O que é", "Um endereço pode esconder o destino real, usar um domínio parecido ou incluir partes enganosas antes do domínio." },
            new[] { "Sinais de alerta", "Domínio inesperado, erros subtis, muitos subdomínios ou caracteres visualmente semelhantes. Um encurtador pode esconder o destino, mas não prova, por si só, que o link seja fraudulento." },
            new[] { "Exemplo fictício", "Um endereço como «banco.exemplo.invalid.site» não pertence ao domínio «exemplo.invalid»." },
            new[] { "Como prevenir", "Não confie apenas no nome visível. Aceda ao serviço digitando o endereço oficial conhecido; não abra links inesperados." }
        });
    }
}

internal sealed class SafetyGuideControl : SecurityContentPage
{
    public SafetyGuideControl()
    {
        AddCard("Recebi uma mensagem suspeita", new[]
        {
            new[] { "O que fazer", "Não responda, não carregue em links e não abra anexos. Faça uma pausa e confirme quem enviou por um canal independente." },
            new[] { "Como confirmar", "Abra a aplicação oficial ou contacte a entidade através de um número ou endereço que já conheça, não dos dados de contacto da mensagem." },
            new[] { "Se a plataforma permitir", "Denuncie a mensagem suspeita e bloqueie o remetente depois de confirmar que não precisa de responder por um canal legítimo." }
        });
        AddCard("Pediram-me um código de autenticação", new[]
        {
            new[] { "O que fazer", "Não partilhe o código nem o reencaminhe. Feche a conversa e consulte a conta pela aplicação ou site oficial." },
            new[] { "Se já partilhou o código", "Aceda ao serviço por um canal oficial, altere a palavra-passe se a conta puder ter sido acedida e termine sessões que não reconhece. Reveja também a atividade recente." },
            new[] { "Se o código era bancário", "Contacte o banco imediatamente através da aplicação oficial ou de um número que já conheça." },
            new[] { "Evite", "Dar o código a alguém que afirma estar a cancelar uma operação ou a proteger a sua conta." }
        });
        AddCard("Pediram-me dados bancários", new[]
        {
            new[] { "O que fazer", "Não envie PIN, palavra-passe, CVV ou códigos por mensagem. Termine o contacto e use a aplicação oficial do banco." },
            new[] { "Se já partilhou dados", "Contacte o banco imediatamente através da aplicação oficial ou do número impresso no seu cartão. Explique que dados partilhou e pergunte que medidas deve tomar." },
            new[] { "Evite", "Continuar a conversa ou usar contactos fornecidos por quem pediu os dados." }
        });
        AddCard("Recebi uma cobrança inesperada", new[]
        {
            new[] { "O que fazer", "Não pague através do link recebido. Consulte diretamente a aplicação ou o site oficial do serviço para verificar se a cobrança existe." },
            new[] { "Antes de pagar", "Confirme o destinatário, o valor e a referência por um canal conhecido. Se não conseguir confirmar, não avance." },
            new[] { "Se já pagou", "Contacte o banco ou o serviço de pagamento rapidamente por um canal oficial e pergunte se a operação pode ser cancelada ou contestada." },
            new[] { "Evite", "Deixar que um prazo curto o impeça de confirmar a cobrança." }
        });
        AddCard("Cliquei num link suspeito", new[]
        {
            new[] { "Se apenas abriu a página", "Feche-a e não introduza dados nem descarregue ficheiros. Um clique, por si só, não confirma que o dispositivo tenha sido comprometido." },
            new[] { "Se introduziu uma palavra-passe", "Aceda ao serviço pela aplicação ou endereço oficial, altere a palavra-passe e termine sessões que não reconhece. Altere também outras contas onde reutilizou essa palavra-passe." },
            new[] { "Se descarregou ou executou um ficheiro", "Não volte a abri-lo. Use a ferramenta de segurança já instalada ou peça ajuda a um técnico de confiança." },
            new[] { "Se introduziu dados de pagamento", "Contacte o banco através de um canal oficial conhecido e explique o que aconteceu." }
        });
        AddCard("Partilhei informações pessoais", new[]
        {
            new[] { "O que fazer", "Identifique que informação partilhou e com quem. Se incluir dados bancários ou credenciais, contacte imediatamente a entidade relevante através de um canal oficial conhecido." },
            new[] { "Proteja as contas", "Altere credenciais expostas, termine sessões que não reconhece e ative medidas de segurança adicionais quando disponíveis. Se reutilizou a palavra-passe, altere-a também nas outras contas." },
            new[] { "Evite", "Enviar mais documentos ou dados a quem iniciou o contacto sem verificar a identidade." }
        });
        AddCard("Suspeito de uma falsa mensagem do banco", new[]
        {
            new[] { "O que fazer", "Não responda nem use links ou números da mensagem. Abra a aplicação oficial ou ligue para um número que já conheça." },
            new[] { "Se existir uma operação que não reconhece", "Contacte o banco imediatamente por um canal oficial, explique o que aconteceu e pergunte que medidas deve tomar." },
            new[] { "Evite", "Instalar aplicações, partilhar códigos ou transferir dinheiro por instrução de um contacto inesperado." }
        });
        AddCard("Quero denunciar uma tentativa de burla", new[]
        {
            new[] { "O que fazer", "Use a ferramenta de denúncia da plataforma ou do serviço envolvido. Se procurar a entidade competente, encontre os respetivos canais oficiais de forma independente." },
            new[] { "Se houve perda de dinheiro ou dados", "Contacte primeiro o banco ou serviço afetado por um canal conhecido e peça orientação sobre os passos seguintes." },
            new[] { "Antes de enviar informação", "Confirme que está num canal oficial e partilhe apenas o necessário. Não confie em contactos ou formulários enviados pelo suspeito." }
        });
    }
}
