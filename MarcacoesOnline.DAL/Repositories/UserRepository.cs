using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MarcacoesOnline.Interfaces;
using MarcacoesOnline.Model;
using Microsoft.EntityFrameworkCore;

namespace MarcacoesOnline.DAL.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly MarcacoesOnlineDbContext _context;

        public UserRepository(MarcacoesOnlineDbContext context)
        {
            _context = context;
        }

        public async Task<User?> GetByIdAsync(int id) =>
            await _context.Users.FindAsync(id);

        public async Task<IEnumerable<User>> GetAllAsync() =>
            await _context.Users.ToListAsync();

        public async Task AddAsync(User user) =>
            await _context.Users.AddAsync(user);

        public void Update(User user) =>
            _context.Users.Update(user);

        public void Delete(User user) =>
            _context.Users.Remove(user);

        public async Task SaveChangesAsync() =>
            await _context.SaveChangesAsync();

        public async Task<User?> GetByEmailAsync(string email)
        {
            return await _context.Users
                .Include(u => u.Pedidos)
                .ThenInclude(p => p.ActosClinicos)
                .FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<bool> ExistsByEmailAsync(string email, int excluirId = 0)
        {
            return await _context.Users.AnyAsync(u => u.Email == email && u.Id != excluirId);
        }

        public async Task<int> CountByTelemovelAsync(string telemovel, int excluirId = 0)
        {
            return await _context.Users.CountAsync(u => u.Telemovel == telemovel && u.Id != excluirId);
        }

    }

}
