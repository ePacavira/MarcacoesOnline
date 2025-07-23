using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MarcacoesOnline.DAL;
using MarcacoesOnline.Interfaces;
using MarcacoesOnline.Model;
using Microsoft.EntityFrameworkCore;

public class PedidoMarcacaoRepository : IPedidoMarcacaoRepository
{
    private readonly MarcacoesOnlineDbContext _context;

    public PedidoMarcacaoRepository(MarcacoesOnlineDbContext context)
    {
        _context = context;
    }

    public async Task<PedidoMarcacao?> GetByIdAsync(int id) =>
        await _context.PedidosMarcacao
            .Include(p => p.ActosClinicos)
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.Id == id);

    public async Task<IEnumerable<PedidoMarcacao>> GetAllAsync() =>
        await _context.PedidosMarcacao
            .Include(p => p.ActosClinicos)
            .Include(p => p.User)
            .ToListAsync();

    public async Task AddAsync(PedidoMarcacao pedido) =>
        await _context.PedidosMarcacao.AddAsync(pedido);

    public void Update(PedidoMarcacao pedido) =>
        _context.PedidosMarcacao.Update(pedido);

    public void Delete(PedidoMarcacao pedido) =>
        _context.PedidosMarcacao.Remove(pedido);

    public async Task<PedidoMarcacao> GetByCodigoReferenciaAsync(string codigoReferencia)
    {
        return await _context.PedidosMarcacao
            .Include(p => p.User)
            .Include(p => p.ActosClinicos)
            .FirstOrDefaultAsync(p => p.CodigoReferencia.ToUpper() == codigoReferencia.ToUpper());
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }

    public async Task<bool> ExisteMarcacaoNoIntervalo(int userId, DateTime? dataInicioPreferida, DateTime? dataFimPreferida)
    {
        return await _context.PedidosMarcacao.AnyAsync(p =>
            p.UserId == userId &&
            (p.Estado == EstadoPedido.Pedido || p.Estado == EstadoPedido.Agendado) && // ← Filtro adicionado
            p.DataInicioPreferida <= dataFimPreferida &&
            p.DataFimPreferida >= dataInicioPreferida
        );
    }

    public async Task<bool> ExisteMarcacaoParaProfissionalNoIntervalo(string profissional, DateTime? dataInicio, DateTime? dataFim)
    {
        return await _context.PedidosMarcacao
            .Include(p => p.ActosClinicos)
            .Where(p =>
                p.Estado == EstadoPedido.Agendado && // ou Estado == 2 se for int
                p.DataInicioPreferida <= dataFim &&
                p.DataFimPreferida >= dataInicio)
            .AnyAsync(p => p.ActosClinicos.Any(a => a.Profissional == profissional));
    }

}
