using System;
using System.Net;
using System.Net.Mail;

public class EmailService
{
    private readonly AppSettings _config;

    public EmailService(AppSettings config)
    {
        _config = config;
    }

    public void EnviarAlerta(string ativo, decimal precoAtual, string tipoAlerta)
    {
        try
        {
            MailMessage mail = new MailMessage();
            mail.From = new MailAddress(_config.Smtp.Usuario);
            mail.To.Add(_config.EmailDestino);
            mail.Subject = $"Alerta de preço do ativo {ativo} - {tipoAlerta}";
            mail.Body = $"O preço atual do ativo {ativo} é de R$ {precoAtual:F2}. Tipo de alerta: {tipoAlerta}";

            using SmtpClient smtp = new SmtpClient(_config.Smtp.Servidor, _config.Smtp.Porta);
            smtp.Credentials = new NetworkCredential(_config.Smtp.Usuario, _config.Smtp.Senha);
            smtp.EnableSsl = _config.Smtp.UsarSsl;

            smtp.Send(mail);
            Console.WriteLine($"E-mail de alerta enviado com sucesso para {_config.EmailDestino}.");
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
}