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

    public async Task SaveChangesAsync() =>
        await _context.SaveChangesAsync();
}
