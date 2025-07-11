using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarcacoesOnline.DTO
{
    public class UpdateProfileDto
    {
        public string? NomeCompleto { get; set; }
        public string? Email { get; set; }
        public string? Telemovel { get; set; }
        public string? Morada { get; set; }
        public DateTime? DataNascimento { get; set; }
        public string? Genero { get; set; }
    }
}


