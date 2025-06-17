using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MarcacoesOnline.Interfaces.Services;
using MarcacoesOnline.Interfaces;
using MarcacoesOnline.Model;
using MarcacoesOnline.DTO;

namespace MarcacoesOnline.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _repo;

        public UserService(IUserRepository repo)
        {
            _repo = repo;
        }

        public async Task<IEnumerable<User>> GetAllAsync()
            => await _repo.GetAllAsync();

        public async Task<User?> GetByIdAsync(int id)
            => await _repo.GetByIdAsync(id);

        public async Task<User> CreateAsync(User user)
        {
            await _repo.AddAsync(user);
            await _repo.SaveChangesAsync();
            return user;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var user = await _repo.GetByIdAsync(id);
            if (user == null) return false;
            _repo.Delete(user);
            await _repo.SaveChangesAsync();
            return true;
        }

        public async Task<bool> PromoverParaRegistadoAsync(int userId, PromoverUserDto dto)
        {
            var user = await _repo.GetByIdAsync(userId);
            if (user == null || user.Perfil != Perfil.Anonimo)
                return false;

           user.NomeCompleto = dto.NomeCompleto;
            user.Email = dto.Email;
            user.Telemovel = dto.Telemovel;
            user.Morada = dto.Morada;
            user.DataNascimento = dto.DataNascimento;
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);
            user.Perfil = Perfil.Registado;

            _repo.Update(user);
            await _repo.SaveChangesAsync();

            return true;
        }

        public async Task<User?> LoginAsync(LoginDto dto)
        {
            var user = await _repo.GetByEmailAsync(dto.Email);
            if (user == null || user.Perfil != Perfil.Registado)
                return null;

           
        if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
                return null;

            return user;
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            return await _repo.GetByEmailAsync(email);
        }
    }
}
