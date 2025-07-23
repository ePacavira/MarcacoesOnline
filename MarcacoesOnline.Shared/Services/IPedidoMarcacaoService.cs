using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MarcacoesOnline.Model;

public interface IPedidoMarcacaoService
{
    Task<IEnumerable<PedidoMarcacao>> GetAllAsync();
    Task<PedidoMarcacao?> GetByIdAsync(int id);
    Task<PedidoMarcacao> CreateAsync(PedidoMarcacao pedido);
    Task<bool> UpdateAsync(int id, PedidoMarcacao pedido);
    Task<bool> DeleteAsync(int id);
    Task<(bool Sucesso, string? Erro)> MarcarComoRealizadoAsync(int id);

}

