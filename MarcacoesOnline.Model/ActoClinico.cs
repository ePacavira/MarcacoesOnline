using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarcacoesOnline.Model
{
    public class ActoClinico
    {
        public int Id { get; set; }
        public string Tipo { get; set; } // Consulta ou Exame
        public string SubsistemaSaude { get; set; } // Medis, SNS, etc.
        public string? Profissional { get; set; }

        public int PedidoMarcacaoId { get; set; }
        public PedidoMarcacao? PedidoMarcacao { get; set; }
    }

}
