    using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MarcacoesOnline.Model;

namespace MarcacoesOnline.Interfaces
{
    public interface IUserRepository
    {
        Task<User?> GetByIdAsync(int id);
        Task<IEnumerable<User>> GetAllAsync();
        Task AddAsync(User user);
        void Update(User user);
        void Delete(User user);
        Task SaveChangesAsync();
        Task<User?> GetByEmailAsync(string email);
        Task<bool> ExistsByEmailAsync(string email, int id);
        Task<int> CountByTelemovelAsync(string telemovel, int id);
    }
}
