using System;
using System.Globalization;
using System.Net.Http;
using System.Threading.Tasks;

// 1. Validação de argumentos de linha de comando
if (args.Length != 3)
{
    Console.WriteLine("Erro: Informe exatamente 3 argumentos.");
    Console.WriteLine("Exemplo: dotnet run -- PETR4 42,34 42,32");
    return;
}

string ativo = args[0];

if (!decimal.TryParse(args[1], NumberStyles.Any, CultureInfo.InvariantCulture, out decimal venda) ||
    !decimal.TryParse(args[2], NumberStyles.Any, CultureInfo.InvariantCulture, out decimal compra))
{
    Console.WriteLine("Erro: Os preços de venda e compra devem ser números válidos.");
    return;
}

Console.WriteLine($"Valores recebidos!\nAtivo: {ativo}\nVenda: R$ {venda:F2}\nCompra: R$ {compra:F2}\n");

// 2. Carrega as configurações
AppSettings config;
try
{
    config = ConfigManager.CarregarConfiguracao();
}
catch (Exception ex)
{
    Console.WriteLine($"Erro ao carregar arquivo de configuração: {ex.Message}");
    return;
}

// 3. Inicializa os serviços
BrapiService brapiService = new BrapiService(config.BrapiToken);
EmailService emailService = new EmailService(config);

string estadoAtual = "NORMAL";

// 4. Loop de monitoramento contínuo
while (true)
{
    try
    {
        decimal precoAtual = await brapiService.ObterCotacaoAsync(ativo);
        Console.WriteLine($"Cotação atual de {ativo}: R$ {precoAtual:F2}");

        if (precoAtual > venda)
        {
            if (estadoAtual != "VENDA")
            {
                Console.WriteLine($"Disparando email de venda! Preço R$ {precoAtual:F2} maior que o limite R$ {venda:F2}.");
                emailService.EnviarAlerta(ativo, precoAtual, "VENDA");
                estadoAtual = "VENDA";
            }
            else
            {
                Console.WriteLine("O preço continua acima do limite. E-mail de VENDA já foi enviado.");
            }
        }
        else if (precoAtual < compra)
        {
            if (estadoAtual != "COMPRA")
            {
                Console.WriteLine($"Disparando email de compra! Preço R$ {precoAtual:F2} menor que o limite R$ {compra:F2}.");
                emailService.EnviarAlerta(ativo, precoAtual, "COMPRA");
                estadoAtual = "COMPRA";
            }
            else
            {
                Console.WriteLine("O preço continua abaixo do limite. E-mail de COMPRA já foi enviado.");
            }
        }
        else
        {
            if (estadoAtual != "NORMAL")
            {
                Console.WriteLine($"O preço do ativo {ativo} voltou para a faixa normal.");
                estadoAtual = "NORMAL";
            }
            else
            {
                Console.WriteLine($"O preço do ativo {ativo} está dentro da faixa desejada.");
            }
        }
    }
    catch (HttpRequestException e)
    {
        Console.WriteLine($"Erro de conexão com a API: {e.Message}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Falha ao checar o preço: {ex.Message}");
    }

    Console.WriteLine("Esperando para ver se o preço mudou...\n");
    await Task.Delay(TimeSpan.FromMinutes(0.5));
}