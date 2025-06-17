using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MarcacoesOnline.Interfaces;
using MarcacoesOnline.Model;

public class PedidoMarcacaoService : IPedidoMarcacaoService
{
    private readonly IPedidoMarcacaoRepository _pedidoRepo;

    public PedidoMarcacaoService(IPedidoMarcacaoRepository pedidoRepo)
    {
        _pedidoRepo = pedidoRepo;
    }

    public async Task<IEnumerable<PedidoMarcacao>> GetAllAsync()
        => await _pedidoRepo.GetAllAsync();

    public async Task<PedidoMarcacao?> GetByIdAsync(int id)
        => await _pedidoRepo.GetByIdAsync(id);

    public async Task<PedidoMarcacao> CreateAsync(PedidoMarcacao pedido)
    {
        await _pedidoRepo.AddAsync(pedido);
        await _pedidoRepo.SaveChangesAsync();
        return pedido;
    }

    public async Task<bool> UpdateAsync(int id, PedidoMarcacao pedido)
    {
        var existing = await _pedidoRepo.GetByIdAsync(id);
        if (existing == null) return false;

        // Atualiza os campos necessários
        existing.Estado = pedido.Estado;
        existing.DataAgendada = pedido.DataAgendada;
        existing.DataInicioPreferida = pedido.DataInicioPreferida;
        existing.DataFimPreferida = pedido.DataFimPreferida;
        existing.HorarioPreferido = pedido.HorarioPreferido;
        existing.Observacoes = pedido.Observacoes;

        _pedidoRepo.Update(existing);
        await _pedidoRepo.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var pedido = await _pedidoRepo.GetByIdAsync(id);
        if (pedido == null) return false;

        _pedidoRepo.Delete(pedido);
        await _pedidoRepo.SaveChangesAsync();
        return true;
    }
}

