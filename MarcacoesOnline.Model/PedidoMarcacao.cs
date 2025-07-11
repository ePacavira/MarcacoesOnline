using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarcacoesOnline.Model
{
    public enum EstadoPedido
    {
        Pedido,
        Agendado,
        Realizado,
        Cancelado
    }

    public class PedidoMarcacao
    {
        public int Id { get; set; }
        public EstadoPedido Estado { get; set; }
        public DateTime? DataAgendada { get; set; }

        public DateTime? DataInicioPreferida { get; set; }
        public DateTime? DataFimPreferida { get; set; }
        public string? HorarioPreferido { get; set; }
        public string? Observacoes { get; set; }

        public int UserId { get; set; }
        public User? User { get; set; }

        public ICollection<ActoClinico> ActosClinicos { get; set; }
        public string CodigoReferencia { get; set; } = string.Empty;
    }

}
