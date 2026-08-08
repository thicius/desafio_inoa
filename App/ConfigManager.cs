using System;
using System.IO;
using System.Text.Json;

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

public static class ConfigManager
{
    public static AppSettings CarregarConfiguracao(string caminhoArquivo = "appsettings.json")
    {
        if (!File.Exists(caminhoArquivo))
        {
            throw new FileNotFoundException($"O arquivo '{caminhoArquivo}' não foi encontrado.");
        }

        string jsonTexto = File.ReadAllText(caminhoArquivo);
        return JsonSerializer.Deserialize<AppSettings>(jsonTexto)
            ?? throw new Exception($"Não foi possível carregar as configurações do arquivo {caminhoArquivo}.");
    }
}