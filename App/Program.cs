using System.IO;
using System.Text.Json;
using System.Net;
using System.Net.Mail;
using System.Net.Http;

// verifica se foram passados exatamente 3 argumentos na linha de comando
if (args.Length != 3)
{
    Console.WriteLine("Erro: Informe exatamente 3 argumentos.");
    Console.WriteLine("Exemplo: dotnet run PETR4 42,34 42,32");
    return;
}

// recebe os valores
string ativo = args[0];
decimal venda = decimal.Parse(args[1]);
decimal compra = decimal.Parse(args[2]);
Console.WriteLine($"Valores recebidos!\nAtivo: {ativo}\nVenda: R$ {venda:F2}\nCompra: R$ {compra:F2}\n");

// lê o appsettings.json com as configurações do e-mail
string jsonTexto = File.ReadAllText("appsettings.json");
AppSettings config = JsonSerializer.Deserialize<AppSettings>(jsonTexto) 
    ?? throw new Exception("Não foi possível carregar as configurações do arquivo appsettings.json.");

// API que eu decidi usar para pegar a cotação do ativo
string url = $"https://brapi.dev/api/v2/stocks/quote?symbols={ativo}";
using HttpClient client = new HttpClient();
client.DefaultRequestHeaders.Add("Authorization", $"Bearer {config.BrapiToken}");

// salva o estado atual para evitar spammar o email
string estadoAtual = "NORMAL"; 

while (true)
{
    try
    {
        string json_data = await client.GetStringAsync(url);

        using JsonDocument doc = JsonDocument.Parse(json_data);
        JsonElement root = doc.RootElement;
        
        decimal precoAtual = root.GetProperty("results")[0].GetProperty("data").GetProperty("regularMarketPrice").GetDecimal();

        Console.WriteLine($"Cotação atual de {ativo}: R$ {precoAtual:F2}");

        if (precoAtual > venda)
        {
            if (estadoAtual != "VENDA")
            {
                Console.WriteLine($"Disparando email de venda! Preço R$ {precoAtual:F2} maior que o limite R$ {venda:F2}.");
                SendAlertEmail(ativo, precoAtual, "VENDA", config);
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
                Console.WriteLine($"Disparando email de compra! Preço R$ {precoAtual:F2} menor que o limite R$ {compra:F2}");
                SendAlertEmail(ativo, precoAtual, "COMPRA", config);
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

void SendAlertEmail(string ativo, decimal precoAtual, string tipoAlerta, AppSettings config)
{
    try
    {
        MailMessage mail = new MailMessage();
        mail.From = new MailAddress(config.Smtp.Usuario);
        mail.To.Add(config.EmailDestino);
        mail.Subject = $"Alerta de preço do ativo {ativo} - {tipoAlerta}";
        mail.Body = $"O preço atual do ativo {ativo} é de R$ {precoAtual:F2}. Tipo de alerta: {tipoAlerta}";

        using SmtpClient smtp = new SmtpClient(config.Smtp.Servidor, config.Smtp.Porta);
        smtp.Credentials = new NetworkCredential(config.Smtp.Usuario, config.Smtp.Senha);
        smtp.EnableSsl = config.Smtp.UsarSsl;

        smtp.Send(mail);
        Console.WriteLine($"E-mail de alerta enviado com sucesso para {config.EmailDestino}.");
    }
    catch (SmtpException e)
    {
        Console.WriteLine($"Erro no servidor SMTP: {e.Message}");
    }
    catch (Exception e)
    {
        Console.WriteLine($"Erro inesperado ao enviar o e-mail: {e.Message}");
    }
}

public class SmtpSettings
{
    public string Servidor { get; set; } = string.Empty;
    public int Porta { get; set; }
    public string Usuario { get; set; } = string.Empty;
    public string Senha { get; set; } = string.Empty;
    public bool UsarSsl { get; set; }
}

public class AppSettings
{
    public string EmailDestino { get; set; } = string.Empty;
    public string BrapiToken { get; set; } = string.Empty;
    public SmtpSettings Smtp { get; set; } = new SmtpSettings();
}