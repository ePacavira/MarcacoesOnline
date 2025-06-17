using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MarcacoesOnline.Interfaces.Services;
using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using MarcacoesOnline.Interfaces.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _config;

    public EmailService(IConfiguration config)
    {
        _config = config;
    }

    public async Task EnviarConfirmacaoAsync(string para, string assunto, string conteudo)
    {
        var smtpConfig = _config.GetSection("Smtp");

        var message = new MailMessage
        {
            From = new MailAddress(smtpConfig["From"]),
            Subject = assunto,
            Body = conteudo,
            IsBodyHtml = false
        };

        message.To.Add(para);

        using var client = new SmtpClient(smtpConfig["Host"], int.Parse(smtpConfig["Port"]!))
        {
            Credentials = new NetworkCredential(smtpConfig["User"], smtpConfig["Pass"]),
            EnableSsl = bool.Parse(smtpConfig["EnableSsl"]!)
        };

        await client.SendMailAsync(message);
    }
}
