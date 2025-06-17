using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace MarcacoesOnline.DTO
{
    public class ActoClinicoAnonimoDto
    {
        [Required]
        public string Tipo { get; set; } = string.Empty;

        [Required]
        public string SubsistemaSaude { get; set; } = string.Empty;

        public string? Profissional { get; set; }
    }

    public class PedidoAnonimoDto
    {
        // Dados do utente
        [Required]
        public string NumeroUtente { get; set; } = string.Empty;

        [Required]
        public string NomeCompleto { get; set; } = string.Empty;

        [Required]
        public DateTime DataNascimento { get; set; }

        [Required]
        public string Genero { get; set; } = string.Empty;

        [Required]
        public string Telemovel { get; set; } = string.Empty;

        [Required]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Morada { get; set; } = string.Empty;

        // Dados do pedido
        [Required]
        public DateTime DataInicioPreferida { get; set; }

        [Required]
        public DateTime DataFimPreferida { get; set; }

        [Required]
        public string HorarioPreferido { get; set; } = string.Empty;

        public string? Observacoes { get; set; }

        [Required]
        public List<ActoClinicoAnonimoDto> ActosClinicos { get; set; } = new();
    }
}