using System.IO;
using System.Text.Json;
using System.Net;
using System.Net.Mail;

// lê os argumentos passados na linha de comando
string ativo = args[0];
float venda = float.Parse(args[1]);
float compra = float.Parse(args[2]);
Console.WriteLine($"Valores recebidos!\n\nAtivo: {ativo}\nVenda: {venda}\nCompra: {compra}");

// lê o arquivo com as configurações do e-mail
string jsonTexto = File.ReadAllText("appsettings.json");
AppSettings config = JsonSerializer.Deserialize<AppSettings>(jsonTexto);

// API que eu decidi usar para pegar a cotação do ativo
string url = $"https://brapi.dev/api/v2/stocks/quote?symbols={ativo}";
using HttpClient client = new HttpClient();

try
{
    string json_data = await client.GetStringAsync(url);

    using JsonDocument doc = JsonDocument.Parse(json_data);
    JsonElement root = doc.RootElement;
    double precoAtual = root.GetProperty("results")[0].GetProperty("data").GetProperty("regularMarketPrice").GetDouble();

    Console.WriteLine($"Cotação atual de {ativo}: R$ {precoAtual:F2}");

    if (precoAtual > venda)
    {
        Console.WriteLine($"Venda! Preço R$ {precoAtual:F2} maior que o limite R$ {venda:F2}. Disparando e-mail...");
        SendAlertEmail(ativo, precoAtual, "VENDA", config);
    }
    else if (precoAtual < compra)
    {
        Console.WriteLine($"Compre! Preço R$ {precoAtual:F2} menor que o limite R$ {compra:F2}. Disparando e-mail...");
        SendAlertEmail(ativo, precoAtual, "COMPRA", config);
    }
    else
    {
        Console.WriteLine($"O preço do ativo {ativo} está dentro da faixa desejada.");
    }
}
catch (Exception)
{
    Console.WriteLine("Deu erro!");
    throw;
}
//  mais um catch?

void SendAlertEmail(string ativo, double precoAtual, string tipoAlerta, AppSettings config)
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
    catch (Exception ex)
    {
        Console.WriteLine($"Erro ao enviar e-mail: {ex.Message}");
    }
}

public class SmtpSettings
{
    public string Servidor { get; set; }
    public int Porta { get; set; }
    public string Usuario { get; set; }
    public string Senha { get; set; }
    public bool UsarSsl { get; set; }
}

public class AppSettings
{
    public string EmailDestino { get; set; }
    public SmtpSettings Smtp { get; set; }
}