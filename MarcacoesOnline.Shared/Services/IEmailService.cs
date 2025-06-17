using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarcacoesOnline.Interfaces.Services
{
    public interface IEmailService
    {
        Task EnviarConfirmacaoAsync(string emailDestino, string assunto, string conteudo);
    }
}
