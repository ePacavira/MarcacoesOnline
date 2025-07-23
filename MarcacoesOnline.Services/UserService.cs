using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MarcacoesOnline.Interfaces.Services;
using MarcacoesOnline.Interfaces;
using MarcacoesOnline.Model;
using MarcacoesOnline.DTO;
using MarcacoesOnline.DAL;
using Microsoft.EntityFrameworkCore;

namespace MarcacoesOnline.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _repo;
        private readonly IEmailService _emailService;
        private readonly MarcacoesOnlineDbContext _context;

        public UserService(IUserRepository repo, IEmailService emailService, MarcacoesOnlineDbContext context)
        {
            _repo = repo;
            _emailService = emailService;
            _context = context;
        }

        public async Task<IEnumerable<User>> GetAllAsync()
            => await _repo.GetAllAsync();

        public async Task<User?> GetByIdAsync(int id) => await _context.Users
            .Include(u => u.Pedidos)
            .ThenInclude(p => p.ActosClinicos)
            .FirstOrDefaultAsync(u => u.Id == id);

        public async Task<User> CreateAsync(User user)
        {
            // 1. Nome completo obrigatório
            if (string.IsNullOrWhiteSpace(user.NomeCompleto))
                throw new Exception("O nome completo é obrigatório.");

            // 2. Email obrigatório e único
            if (string.IsNullOrWhiteSpace(user.Email))
                throw new Exception("O e-mail é obrigatório.");

            if (await _repo.ExistsByEmailAsync(user.Email, user.Id))
                throw new Exception("Este e-mail já está em uso.");

            // 3. Telemóvel - permitir no máximo 3 contas por número
            if (!string.IsNullOrWhiteSpace(user.Telemovel))
            {
                var totalComMesmoNumero = await _repo.CountByTelemovelAsync(user.Telemovel, user.Id);
                if (totalComMesmoNumero >= 3)
                    throw new Exception("Este número de telemóvel já está associado ao número máximo de contas permitido (3).");
            }

            // 4. Password mínima de 6 caracteres
            if (string.IsNullOrWhiteSpace(user.PasswordHash) || user.PasswordHash.Length < 6)
                throw new Exception("A password deve ter pelo menos 6 caracteres.");

            // 5. Data de nascimento válida e com pelo menos 20 anos
            if (user.DataNascimento == default)
                throw new Exception("A data de nascimento é obrigatória.");

            var hoje = DateTime.Today;
            var idade = hoje.Year - user.DataNascimento.Year;
            if (user.DataNascimento.Date > hoje.AddYears(-idade)) idade--;

            if (idade < 20)
                throw new Exception("O utilizador deve ter pelo menos 20 anos de idade.");


            // 6. Morada obrigatória
            if (string.IsNullOrWhiteSpace(user.Morada))
                throw new Exception("A morada é obrigatória.");

            // Tudo validado: criar utilizador
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(user.PasswordHash);
            user.Perfil = Perfil.Registado;
            user.FotoPath ??= string.Empty;

            await _repo.AddAsync(user);
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

            if (await _repo.ExistsByEmailAsync(dto.Email, user.Id))
                throw new Exception("Este e-mail já está em uso.");

            var totalComMesmoNumero = await _repo.CountByTelemovelAsync(dto.Telemovel, user.Id);
            if (totalComMesmoNumero >= 3)
                throw new Exception("Este número de telemóvel já está associado ao número máximo de contas permitido (3).");

            var password = GerarPassword();

            user.NomeCompleto = dto.NomeCompleto;
            user.Email = dto.Email;
            user.Telemovel = dto.Telemovel;
            user.Morada = dto.Morada;
            user.DataNascimento = dto.DataNascimento;
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
            user.Perfil = Perfil.Registado;

            _repo.Update(user);
            await _repo.SaveChangesAsync();

            if (!string.IsNullOrWhiteSpace(user.Email))
            {
                var mensagem = $"""
                Olá {user.NomeCompleto},

                A sua conta foi criada com sucesso na plataforma de marcações online.

                Aqui estão os seus dados de acesso:
                - Email: {user.Email}
                - Palavra-passe: {password}

                Pode agora aceder ao sistema e acompanhar as suas marcações.

                Obrigado,
                Equipa de Atendimento
                """;

                await _emailService.EnviarConfirmacaoAsync(
                    user.Email,
                    "Dados de Acesso à Plataforma de Marcação",
                    mensagem
                );
            }

            return true;
        }

        public static string GerarPassword(int tamanho = 10)
        {
            const string caracteres = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789@#$!";
            var random = new Random();
            return new string(Enumerable.Repeat(caracteres, tamanho)
                .Select(s => s[random.Next(s.Length)]).ToArray());
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

        public async Task<bool> UpdateAsync(int id, User userAtualizado)
        {
            var userExistente = await _repo.GetByIdAsync(id);
            if (userExistente == null)
                return false;

            // Atualiza apenas os campos relevantes
            userExistente.NomeCompleto = userAtualizado.NomeCompleto ?? userExistente.NomeCompleto;
            userExistente.Email = userAtualizado.Email ?? userExistente.Email;
            userExistente.Telemovel = userAtualizado.Telemovel ?? userExistente.Telemovel;
            userExistente.Morada = userAtualizado.Morada ?? userExistente.Morada;
            userExistente.DataNascimento = userAtualizado.DataNascimento != DateTime.MinValue ? userAtualizado.DataNascimento : userExistente.DataNascimento;
            userExistente.FotoPath = userAtualizado.FotoPath ?? userExistente.FotoPath;

            _repo.Update(userExistente);
            await _repo.SaveChangesAsync();

            return true;
        }

        public async Task<User?> GetUserByIdAsync(int id)
        {
            return await _repo.GetByIdAsync(id);
        }

        public async Task UpdateUserPhotoAsync(int userId, string? photoPath)
        {
            var user = await _repo.GetByIdAsync(userId);
            if (user == null)
                throw new Exception("Utilizador não encontrado");

            user.FotoPath = photoPath ?? string.Empty;
            _repo.Update(user);
            await _repo.SaveChangesAsync();
        }
    }


}
