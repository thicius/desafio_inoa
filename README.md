# Desafio Inoa: Stock Quote Alert

Script que monitora continuamente a cotação de um ativo da B3 e permite enviar um alerta por e-mail quando a cotação está abaixo do valor de compra ou acima do valor de venda. 

O usuário passa três argumentos pela linha de comando:

1. Ativo que será monitorado;
2. Preço definido para a venda;
3. Preço definido para a compra.

Nesta exata ordem, por exemplo:
dotnet run -- PETR4 42,34 42,32

A consulta à cotação do ativo é feita através da API da [Brapi](https://brapi.dev/), e salva a última consulta feita para evitar spam de e-mail nos casos onde a cotação fica por muito tempo fora do intervalo dado.

---

# Linha de raciocínio na resolução do problema

As principais perguntas que levaram a resolução deste problema surgiram aproximadamente nesta ordem:

1. Como receber os argumentos dados na linha de comando?
2. Como enviar um email usando C# e o protocolo SMTP?
3. Como vou pegar continuamente os dados com valores atualizados da cotação de um ativo? (API)
4. Com o preço atual em mãos, qual vai ser a estrutura do código que vai disparar o email a depender de cada caso?

* Complementei colocando certos *warnings* pra diversos tipos de erros, mas apenas para os que imaginei serem os mais comuns.

---

## 1. Recebendo os argumentos da linha de comando

Como o programa deve ser executado através da linha de comando, a primeira coisa a resolver é a forma como o programa recebe os três valores definidos no enunciado, o que é feito por meio da variável `args`.

Inicialmente eu havia postos varias verificações: se o usuário está passando três argumentos, se os dois últimos são numéricos, se está usando vírgula ou ponto. Mas no fim, por simplicidade eu deixei apenas o seguinte:

```csharp
if (args.Length != 3)
{
    Console.WriteLine("Erro: Informe exatamente 3 argumentos.");
    Console.WriteLine("Exemplo: dotnet run -- PETR4 42,34 42,32");
    return;
}
```

Depois disso, os valores poderiam ser acessados através de suas posições:

```csharp
string ativo = args[0];
decimal venda = decimal.Parse(args[1], CultureInfo.InvariantCulture);
decimal compra = decimal.Parse(args[2], CultureInfo.InvariantCulture);
```

Escolhi `decimal` para os preços porque tem mais precisão que `float` ou `double`.

---

## 2. Enviando um e-mail através do C#

A segunda questão foi descobrir como fazer o programa enviar um e-mail. Nesta etapa algumas referências na internet foram muito úteis, dentre elas:
- [Como Enviar E-mails com C# via SMTP - EP5 C# Na Prática
](https://youtu.be/OGuQu13OiZk?si=hGX6qzb1YZJloyHL)
-[SmtpClient.Send Método
](https://learn.microsoft.com/pt-br/dotnet/api/system.net.mail.smtpclient.send?view=net-10.0) 

Os links acima são antigos e de fato o `System.Net.Mail.SmtpClient` está obsoleto, mas serviram para esta tarefa.

Um primeiro teste poderia ser feito criando a mensagem diretamente no código:

```csharp
MailMessage mail = new MailMessage();

mail.From = new MailAddress(usuario);
mail.To.Add(destinatario);
mail.Subject = "Alerta";
mail.Body = "Mensagem de teste";
```

Depois é necessário configurar o servidor SMTP:

```csharp
using SmtpClient smtp = new SmtpClient(servidor, porta);

smtp.Credentials = new NetworkCredential(usuario, senha);
smtp.EnableSsl = true;

smtp.Send(mail);
```

### Configuração através do `appsettings.json`

Criei o `appsettings.json` com a seguinte estrutura:

```json
{
  "EmailDestino": "destinatario@example.com",
  "BrapiToken": "SEU_TOKEN",
  "Smtp": {
    "Servidor": "smtp.example.com",
    "Porta": 587,
    "Usuario": "usuario@example.com",
    "Senha": "SUA_SENHA",
    "UsarSsl": true
  }
}
```

O arquivo é lido pelo programa:

```csharp
string jsonTexto = File.ReadAllText("appsettings.json");

AppSettings config =
    JsonSerializer.Deserialize<AppSettings>(jsonTexto)
    ?? throw new Exception(
        "Não foi possível carregar as configurações do arquivo appsettings.json.");
```

Para transformar o JSON em objetos, criei as classes `AppSettings` e `SmtpSettings`.
Então até aqui, já temos uma maneira de fazer o programa enviar um e-mail sem deixar as configurações diretamente no código.

---

## 3. Consultando continuamente a cotação através de uma API

Nesta etapa, depois de uma rápida pesquisa decidi utilizar a API da [Brapi](https://brapi.dev/) para disponibilizar os dados de ações da B3.

A URL da consulta depende do ativo recebido na linha de comando:

```csharp
string url =
    $"https://brapi.dev/api/v2/stocks/quote?symbols={ativo}";
```

Como a API utiliza um token de acesso, coloquei esse token no `appsettings.json`, junto das outras configuraçõe.
Fiz os testes com uma conta gratuita na Brapi que tem alguns limites, como o número de requisições por mês.

O token é enviado no cabeçalho da requisição:

```csharp
client.DefaultRequestHeaders.Add(
    "Authorization",
    $"Bearer {config.BrapiToken}");
```

### Interpretando a resposta da API

A resposta da Brapi é um JSON. Assim, depois de realizar a requisição precisei descobrir como acessar dentro desse JSON exatamente o campo que continha a cotação.

Utilizei `JsonDocument` para interpretar a resposta:

```csharp
using JsonDocument doc = JsonDocument.Parse(json_data);
JsonElement root = doc.RootElement;
```

A partir da estrutura dada pela API, o preço atual é obtido através de:

```csharp
decimal precoAtual =
    root.GetProperty("results")[0]
        .GetProperty("data")
        .GetProperty("regularMarketPrice")
        .GetDecimal();
```

Depois disso, finalmente tinha no programa as três informações necessárias para resolver o problema.

---

## 4. Estrutura da regra de compra, venda e monitoramento

Com os três preços disponíveis, a estrutura principal do programa passa a ser algo como:

Se o preço atual for maior que o preço de venda, o programa deve enviar um alerta de venda.
Se o preço atual for menor que o preço de compra, devemos enviar um alerta de compra.
E no caso contrário, o preço estaria dentro do intervalo definido e nada precisa ser feito.

Em código:

```csharp
if (precoAtual > venda)
{
    // VENDA
}
else if (precoAtual < compra)
{
    // COMPRA
}
else
{
    // dentro da faixa
}
```

Como o programa deve continuar monitorando enquanto estiver rodando, basta acolocar essa consulta dentro de um loop `while (true)`:

```csharp
while (true)
{
    // consulta a API
    // verifica o preço
    // envia alerta se necessário

    await Task.Delay(TimeSpan.FromMinutes(0.5));
}
```
Atualmente, o programa espera 30 segundos entre cada consulta. 
O valor pode ser alterado diretamente no código caso seja necessário utilizar outro intervalo.

### Evitando o envio repetido de e-mails

Note que se o preço permanecesse acima do limite de venda durante vários minutos, o programa enviaria um e-mail a cada consulta.
Supus que o alerta deve ocorrer **quando o ativo entra** em uma das situações, e não enquanto ele simplesmente continua nela.

Para isso, criei uma variável que representa o estado atual, que começa sendo `NORMAL`, mas pode tomar os valores:

```text
NORMAL
VENDA
COMPRA
```

Quando o preço ultrapassa o limite de venda, o programa verifica se já estava no estado `VENDA`:

```csharp
if (precoAtual > venda)
{
    if (estadoAtual != "VENDA")
    {
        SendAlertEmail(ativo, precoAtual, "VENDA", config);
        estadoAtual = "VENDA";
    }
}
```

Dessa forma, o primeiro cruzamento gera o e-mail e altera o estado para `VENDA`. Nas próximas consultas, enquanto o preço continuar acima do limite, nenhum novo e-mail é enviado. 

O mesmo raciocínio vale pro caso de compra. Mas quando o preço volta para dentro do intervalo, o estado volta para `NORMAL`:

Fiquei em dúvida se deveria mandar um email pro caso em que o preço atual volta para dentro do "intervalo normal", optei por mostrar apenas no console.

Adicionei um tratamento para erros de conexão com a API e no servidor SMTP. A intenção era de informar no console o que aconteceu sem encerrar o monitoramento por causa de uma falha momentânea.

---

# 5. Arquitetura e Organização do Código

Para manter o código limpo a solução foi dividida em quatro arquivos principais, isolando cada papel do sistema:

*   **`ConfigManager.cs`**: Lê o arquivo `appsettings.json` e mapeia as configurações para as classes de modelo (`AppSettings` e `SmtpSettings`).
*   **`BrapiService.cs`**: Isola a comunicação HTTP com a API da Brapi e faz o parse da resposta JSON para extrair o preço atual do ativo.
*   **`EmailService.cs`**: Contém a lógica de montagem e envio de e-mails de alerta via protocolo SMTP.
*   **`Program.cs`**: Recebe os argumentos de linha de comando, inicializa os serviços e executa o loop de monitoramento mantendo o estado (`NORMAL`, `VENDA`, `COMPRA`).

---

# 6. Como executar

## Requisitos

* Sistema compatível com .NET 8;
* .NET SDK 8.0 ou superior;
* Token de acesso da Brapi;
* Conta de e-mail com acesso a um servidor SMTP.

## Configuração

Antes de executar o programa, é necessário criar um arquivo `appsettings.json` e preencher as informações de acesso à Brapi e ao servidor SMTP.

```json
{
  "EmailDestino": "destinatario@example.com",
  "BrapiToken": "SEU_TOKEN",
  "Smtp": {
    "Servidor": "smtp.example.com",
    "Porta": 587,
    "Usuario": "usuario@example.com",
    "Senha": "SUA_SENHA",
    "UsarSsl": true
  }
}
```

## Execução

Na pasta do projeto:

```bash
dotnet restore
```

Depois:

```bash
dotnet run -- PETR4 42.34 42.32
```

Os argumentos devem ser informados na seguinte ordem:

```text
ATIVO PREÇO_DE_VENDA PREÇO_DE_COMPRA
```

# 7. Sobre o Uso de Inteligência Artificial

Durante o desenvolvimento, utilizei ferramentas de Inteligência Artificial principalmente para **tirar dúvidas sobre certas funções do C# e revisar o código desenvolvido**. A lógica de controle de estado para não spammar e-mails, o fluxo de decisão e a arquitetura geral foram desenvolvidas inteiramente por mim.

A implementação e as decisões principais foram feitas durante o desenvolvimento do projeto, enquanto a IA foi utilizada como apoio nas tarefas mencionadas acima e sugerir melhorias.

Grande parte dos tratamentos de erro presentes `catch (Exception e)` na versão final surgiu durante essas revisões.

