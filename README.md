# Rede Segura

A Rede Segura inclui uma aplicação Windows que avalia localmente mensagens potencialmente fraudulentas e um site informativo para explicar a instalação e utilização.

- `index.html`, `styles.css` e `images/`: site informativo e ícone.
- `rede-segura-app.cs`: código-fonte da aplicação Windows.
- `scam-analysis.cs`: motor de análise local, independente da interface.
- `tests/scam-analyzer-tests.cs`: casos de avaliação em português europeu e brasileiro.
- `test-analyzer.ps1`: compila e executa a bateria de testes.
- `build-windows-installer.ps1`: cria `RedeSegura.exe` e `RedeSegura-Setup.exe`.

O site não analisa mensagens; essa funcionalidade está disponível apenas na aplicação Windows.

## Análise de mensagens

O motor normaliza texto Unicode sem perder a referência aos excertos originais, separa frases e procura pedidos de dados secretos, pressão, pagamentos, alegações de entrega ou identidade e URLs. Analisa URLs apenas localmente com `Uri`/IDN: não os abre, não faz pedidos de rede e não confirma a identidade do remetente. Também reconhece emails, telefones e referências de pagamento, mas a mera presença de um valor ou desses elementos não prova fraude.

Os sinais diretos têm estes pesos heurísticos:

| Sinal | Peso |
| --- | ---: |
| Pedido de palavra-passe, código, PIN ou dados pessoais | 5 |
| Ameaça de bloqueio, cancelamento ou outra consequência | 3 |
| Urgência coerciva ou instrução para evitar confirmação | 2 |
| Pedido de pagamento ou transferência | 3 |
| Promessa de prémio/recompensa | 2 |
| Alegação de encomenda retida | 2 |
| Alegação de representar uma entidade | 1 |
| URL encurtado / endereço IP / domínio semelhante ou invulgar | 2 / 3 / 4 / 2 |
| URL sem destino verificado (sinal informativo, sem pontos) | 0 |

Combinações acrescentam um peso contextual (normalmente 2 ou 3); apenas a combinação mais forte é somada, para reduzir dupla contagem de sinais relacionados. A escala resultante não é uma probabilidade:

Um verbo de pagamento/transferência (por exemplo, “envie”, “manda”, “transfira”, “pague” ou “deposite”) próximo de um montante com símbolo ou moeda é reconhecido como pedido de pagamento. Formatos como `43$`, `$43`, `43 USD`, `43€`, `43 EUR` e `1,95 €` são suportados. Se o mesmo período também condicionar o pagamento a continuar, desbloquear, receber, ativar ou libertar algo, é apresentada uma evidência contextual adicional (peso 2). A classificação tende a moderada quando faltam outros sinais; a combinação não prova fraude. Contexto explícito de aplicação/site/portal oficial conhecido pode reduzir o falso positivo e montantes sem um pedido não são classificados como solicitação.

- **Alto risco**: combinação de sinais acionáveis com pontuação mínima de 8, ou pedido de credenciais acompanhado por uma combinação contextual ou URL de IP/domínio enganador.
- **Risco moderado**: pedido acionável ou pontuação de pelo menos 4, sem atingir a regra de alto risco.
- **Baixo risco aparente**: mensagem com contexto suficiente e sem sinais encontrados.
- **Inconclusivo**: mensagem curta, link não verificado isolado ou sinais insuficientes.

As regras são heurísticas e não substituem confirmação independente. Um resultado baixo não garante legitimidade e um resultado alto não prova definitivamente uma burla. Confirme pedidos importantes na aplicação oficial ou através de um contacto que já conheça; não partilhe palavras-passe nem códigos de autenticação. A mensagem é analisada no dispositivo e não é guardada pelo motor. A entrada está limitada a 20 000 caracteres.

## Testes

Execute a bateria no Windows com PowerShell:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\test-analyzer.ps1
```

Os testes incluem exemplos de burlas bancárias e de entregas, prémios, pedidos de códigos e pagamentos condicionados, mensagens legítimas, conversas comuns, contexto educativo e casos ambíguos em PT-PT e PT-BR, além de verificações de excertos, limite de entrada, normalização e entidades. Os rótulos esperados são casos de teste definidos manualmente, portanto passar a bateria não mede a precisão no mundo real nem constitui avaliação independente. Não se declara uma percentagem de eficácia. A bateria inclui regressões para separar avisos de pedidos contraditórios, distinguir um montante de um pedido de pagamento, manter as mensagens legítimas de faturas em risco baixo e não tratar qualquer URL desconhecido como prova de fraude.

## Compilação

Para compilar a aplicação Windows e o instalador, execute:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\build-windows-installer.ps1
```
