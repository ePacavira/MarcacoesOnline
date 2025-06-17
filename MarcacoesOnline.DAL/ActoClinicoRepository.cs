using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MarcacoesOnline.DAL;
using MarcacoesOnline.Interfaces;
using MarcacoesOnline.Model;
using Microsoft.EntityFrameworkCore;

public class ActoClinicoRepository : IActoClinicoRepository
{
    private readonly MarcacoesOnlineDbContext _context;

    public ActoClinicoRepository(MarcacoesOnlineDbContext context)
    {
        _context = context;
    }

    public async Task<ActoClinico?> GetByIdAsync(int id) =>
        await _context.ActosClinicos.FindAsync(id);

    public async Task<IEnumerable<ActoClinico>> GetAllAsync() =>
        await _context.ActosClinicos.ToListAsync();

    public async Task AddAsync(ActoClinico acto) =>
        await _context.ActosClinicos.AddAsync(acto);

    public void Delete(ActoClinico acto) =>
        _context.ActosClinicos.Remove(acto);

    public async Task SaveChangesAsync() =>
        await _context.SaveChangesAsync();
}
