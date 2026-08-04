using System.IO;
using System.Text.Json;

string ativo = args[0];
float venda = float.Parse(args[1]);
float compra = float.Parse(args[2]);

Console.WriteLine($"Valores recebidos!\n\nAtivo: {ativo}\nVenda: {venda}\nCompra: {compra}");

string url = $"https://brapi.dev/api/v2/stocks/quote?symbols={ativo}";

using HttpClient client = new HttpClient();

try
{
    string json_data = await client.GetStringAsync(url);
    Console.WriteLine(json_data);
    Console.WriteLine("");
    Console.WriteLine();

    using JsonDocument doc = JsonDocument.Parse(json_data);
    JsonElement root = doc.RootElement;
    Console.WriteLine($"root = {root}");
    double precoAtual = root.GetProperty("results")[0].GetProperty("data").GetProperty("regularMarketPrice").GetDouble();

    Console.WriteLine($"Cotação atual de {ativo}: R$ {precoAtual:F2}");

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

    string jsonTexto = File.ReadAllText("appsettings.json");
    AppSettings config = JsonSerializer.Deserialize<AppSettings>(jsonTexto);

    if (precoAtual > venda)
    {
        Console.WriteLine($"Venda!\nO preço atual do ativo {ativo} está maior que o preço de venda.");
    }
    else if (precoAtual < compra)
    {
        Console.WriteLine($"Compre!\nO preço atual do ativo {ativo} está menor que o preço de compra.");
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
