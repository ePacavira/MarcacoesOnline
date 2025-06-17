using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MarcacoesOnline.Model;

namespace MarcacoesOnline.DTO
{
    public class PedidoMarcacaoDto
    {
        public DateTime? DataInicioPreferida { get; set; }
        public DateTime? DataFimPreferida { get; set; }
        public string? HorarioPreferido { get; set; }
        public string? Observacoes { get; set; }

        public EstadoPedido Estado { get; set; } = EstadoPedido.Pedido;

        public int UserId { get; set; }
    }

}
