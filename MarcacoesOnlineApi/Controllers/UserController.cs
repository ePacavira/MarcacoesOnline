using Microsoft.AspNetCore.Mvc;
using MarcacoesOnline.Interfaces.Services;
using MarcacoesOnline.Model;
using MarcacoesOnline.DTO;
using Microsoft.AspNetCore.Components.Forms;
using MarcacoesOnline.Services;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using MarcacoesOnline.Interfaces;
using System;

namespace MarcacoesOnlineApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly IUserService _service;
        private readonly IUserRepository _userRepo;
        private readonly IPedidoMarcacaoRepository _pedidoRepo;

        public UserController(IUserService service, IUserRepository userRepo, IPedidoMarcacaoRepository pedidoRepo)
        {
            _service = service;
            _userRepo = userRepo;
            _pedidoRepo = pedidoRepo;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<User>>> GetAll()
            => Ok(await _service.GetAllAsync());

        [HttpGet("{id}")]
        public async Task<ActionResult<User>> GetById(int id)
        {
            var user = await _service.GetByIdAsync(id);
            if (user == null) return NotFound();
            return Ok(user);
        }

        [HttpPost]
        public async Task<ActionResult<User>> Create(User user)
        {
            var novo = await _service.CreateAsync(user);
            return CreatedAtAction(nameof(GetById), new { id = novo.Id }, novo);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var removido = await _service.DeleteAsync(id);
            if (!removido) return NotFound();
            return NoContent();
        }

        [HttpPatch("promover/{id}")]
        public async Task<IActionResult> Promover(int id, [FromBody] PromoverUserDto dto)
        {
            var sucesso = await _service.PromoverParaRegistadoAsync(id, dto);
            if (!sucesso)
                return BadRequest("Usuário não encontrado ou já é registado.");
            return NoContent();
        }

        [Authorize]
        [HttpGet("me-info")]
        public async Task<IActionResult> GetMe()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int userId))
                return Unauthorized("Token inválido.");

            var user = await _service.GetByIdAsync(userId);
            if (user == null) return NotFound();

            return Ok(new
            {
                user.Id,
                user.NomeCompleto,
                user.Email,
                user.Telemovel,
                user.Perfil
            });
        }

        [Authorize(Roles = "Administrador")]
        [HttpGet("admin/todos")]
        public async Task<IActionResult> VerTodosOsUtilizadores()
        {
            var users = await _service.GetAllAsync();
            return Ok(users.Select(u => new
            {
                u.Id,
                u.NomeCompleto,
                u.Email,
                u.Perfil
            }));
        }

        [Authorize]
        [HttpPost("{id}/foto")]
        public async Task<IActionResult> UploadFoto(int id, IFormFile file)
        {
            var user = await _service.GetByIdAsync(id);
            if (user == null)
                return NotFound("Utilizador não encontrado.");

            if (file == null || file.Length == 0)
                return BadRequest("Ficheiro inválido.");

            var ext = Path.GetExtension(file.FileName);
            var nomeFoto = $"utente_{id}{ext}";
            var caminhoPasta = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "fotografias");

            if (!Directory.Exists(caminhoPasta))
                Directory.CreateDirectory(caminhoPasta);

            var caminhoCompleto = Path.Combine(caminhoPasta, nomeFoto);

            using (var stream = new FileStream(caminhoCompleto, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Atualiza caminho no user
            user.FotoPath = $"/fotografias/{nomeFoto}";
            await _service.UpdateAsync(id, user);

            return Ok(new
            {
                mensagem = "Fotografia enviada com sucesso.",
                url = user.FotoPath
            });
        }

        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> GetProfile()
        {
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
                return Unauthorized();

            if (!int.TryParse(userIdClaim.Value, out var userId))
                return Unauthorized();

            var user = await _service.GetByIdAsync(userId);
            if (user == null)
                return NotFound();

            var result = new
            {
                user.Id,
                user.NumeroUtente,
                user.NomeCompleto,
                user.Genero,
                user.DataNascimento,
                user.Email,
                user.Telemovel,
                user.Morada,
                user.Perfil,
                user.FotoPath
            };

            return Ok(result);
        }

        [Authorize]
        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto)
        {
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
                return Unauthorized();

            if (!int.TryParse(userIdClaim.Value, out var userId))
                return Unauthorized();

            var user = await _service.GetByIdAsync(userId);
            if (user == null)
                return NotFound();

            // Atualizar apenas os campos permitidos
            user.NomeCompleto = dto.NomeCompleto ?? user.NomeCompleto;
            user.Email = dto.Email ?? user.Email;
            user.Telemovel = dto.Telemovel ?? user.Telemovel;
            user.Genero = dto.Genero ?? user.Genero;
            user.DataNascimento = dto.DataNascimento ?? user.DataNascimento;
            user.Morada = dto.Morada ?? user.Morada;

            await _service.UpdateAsync(userId, user);

            return Ok(user);
        }

        [Authorize]
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
                return Unauthorized();

            var user = await _userRepo.GetByIdAsync(userId);
            if (user == null)
                return NotFound();

            // Verifica se a password atual está correta
            if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.PasswordHash))
                return BadRequest(new { message = "A palavra-passe atual está incorreta." });

            if (dto.NewPassword != dto.ConfirmPassword)
                return BadRequest(new { message = "A nova palavra-passe e a confirmação não coincidem." });

            // Atualiza a password
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            _userRepo.Update(user);
            await _userRepo.SaveChangesAsync();

            return Ok(new { message = "Palavra-passe alterada com sucesso." });
        }

        [Authorize]
        [HttpGet("pedidos")]
        public async Task<IActionResult> GetPedidosDoUtilizador()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int userId))
                return Unauthorized();

            var user = await _service.GetByIdAsync(userId);
            if (user == null) return NotFound();

            var pedidos = user.Pedidos?
                .Select(p => new
                {
                    p.Id,
                    p.Estado,
                    p.DataAgendada,
                    p.DataInicioPreferida,
                    p.DataFimPreferida,
                    p.HorarioPreferido,
                    p.Observacoes,
                    ActosClinicos = p.ActosClinicos.Select(a => new
                    {
                        a.Id,
                        a.Tipo,
                        a.SubsistemaSaude,
                        a.Profissional
                    }).ToList()
                }).ToList();

            if (pedidos == null)
            {
                return Ok(new object[0]);
            }
            else
            {
                return Ok(pedidos);
            }
        }

        [Authorize]
        [HttpGet("pedidos/{id}")]
        public async Task<IActionResult> GetPedidosByUserId(int id)
        {
            var user = await _service.GetByIdAsync(id);
            if (user == null)
                return NotFound(new { message = "Utilizador não encontrado." });

            var pedidos = user.Pedidos?.Select(p => new
            {
                p.Id,
                p.Estado,
                p.DataAgendada,
                p.DataInicioPreferida,
                p.DataFimPreferida,
                p.HorarioPreferido,
                p.Observacoes,
                ActosClinicos = p.ActosClinicos.Select(a => new
                {
                    a.Id,
                    a.Tipo,
                    a.SubsistemaSaude,
                    a.Profissional
                }).ToList()
        }).ToList();

            if (pedidos == null)
            {
                return Ok(new object[0]);
            }
            else
            {
                return Ok(pedidos);
            }
        }

        [Authorize]
        [HttpDelete("{id}/cancelar")]
        public async Task<IActionResult> CancelarPedido(int id)
        {
            var pedido = await _pedidoRepo.GetByIdAsync(id);
            if (pedido == null)
                return NotFound();

            pedido.Estado = EstadoPedido.Cancelado;
            await _pedidoRepo.SaveChangesAsync();

            return Ok(new { message = "Pedido cancelado com sucesso." });
        }

    }
}
