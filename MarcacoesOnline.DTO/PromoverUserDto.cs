using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarcacoesOnline.DTO
{
    public class PromoverUserDto
    {
        public string NomeCompleto { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Telemovel { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public DateTime DataNascimento { get; set; }
        public string Morada { get; set; } = string.Empty;
    }
}
