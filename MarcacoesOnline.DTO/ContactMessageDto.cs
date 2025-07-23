using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarcacoesOnline.DTO
{
    public class ContactMessageDto
    {
        public string Nome { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Telefone { get; set; } = string.Empty;
        public string Assunto { get; set; } = string.Empty;
        public string Mensagem { get; set; } = string.Empty;
        public bool Newsletter { get; set; }
    }
}
