using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MarcacoesOnline.DTO;
using MarcacoesOnline.Model;

namespace MarcacoesOnline.Interfaces.Services
{
    public interface IUserService
    {
        Task<IEnumerable<User>> GetAllAsync();
        Task<User?> GetByIdAsync(int id);
        Task<User> CreateAsync(User user);
        Task<bool> UpdateAsync(int id, User user);
        Task<bool> DeleteAsync(int id);
        Task<User?> GetByEmailAsync(string email);
        Task<bool> PromoverParaRegistadoAsync(int userId, PromoverUserDto dto);
        Task<User?> LoginAsync(LoginDto dto);
    }
}

