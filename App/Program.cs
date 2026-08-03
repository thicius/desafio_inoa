using System.Text.Json;

// See https://aka.ms/new-console-template for more information
string ativo = args[0];
float venda = float.Parse(args[1]);
float compra = float.Parse(args[2]);
Console.WriteLine($"Programa funcionando!\nVeja {ativo}, {venda}, {compra}");

string url = $"https://brapi.dev/api/v2/stocks/quote?symbols={ativo}";
//  mais um catch?

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
}
catch (Exception)
{
    Console.WriteLine("Deu erro!");
    throw;
}