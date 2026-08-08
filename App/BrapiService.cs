using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

public class BrapiService
{
    private readonly HttpClient _httpClient;

    public BrapiService(string token)
    {
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
    }

    public async Task<decimal> ObterCotacaoAsync(string ativo)
    {
        string url = $"https://brapi.dev/api/v2/stocks/quote?symbols={ativo}";
        string json_data = await _httpClient.GetStringAsync(url);

        using JsonDocument doc = JsonDocument.Parse(json_data);
        JsonElement root = doc.RootElement;

        return root.GetProperty("results")[0]
                   .GetProperty("data")
                   .GetProperty("regularMarketPrice")
                   .GetDecimal();
    }
}