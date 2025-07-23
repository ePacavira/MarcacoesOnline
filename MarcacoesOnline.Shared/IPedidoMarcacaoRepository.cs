using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MarcacoesOnline.Model;

namespace MarcacoesOnline.Interfaces
{
    public interface IPedidoMarcacaoRepository
    {
        Task<PedidoMarcacao?> GetByIdAsync(int id);
        Task<IEnumerable<PedidoMarcacao>> GetAllAsync();
        Task AddAsync(PedidoMarcacao pedido);
        void Update(PedidoMarcacao pedido);
        void Delete(PedidoMarcacao pedido);
        Task<PedidoMarcacao?> GetByCodigoReferenciaAsync(string codigo);
        Task SaveChangesAsync();
        Task<bool> ExisteMarcacaoParaProfissionalNoIntervalo(string profissional, DateTime? dataInicio, DateTime? dataFim);
        Task<bool> ExisteMarcacaoNoIntervalo(int userId, DateTime? dataInicioPreferida, DateTime? dataFimPreferida);
    }
}
