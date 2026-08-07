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

Como o programa deve ser executado através da linha de comando, a primeira coisa a resolver é a forma como o programa recebe os três valores definidos no enunciado.o que é feito por meio da variável `args`.

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
