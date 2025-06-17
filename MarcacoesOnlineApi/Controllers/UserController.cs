using Microsoft.AspNetCore.Mvc;
using MarcacoesOnline.Interfaces.Services;
using MarcacoesOnline.Model;
using MarcacoesOnline.DTO;
using Microsoft.AspNetCore.Components.Forms;
using MarcacoesOnline.Services;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace MarcacoesOnlineApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly IUserService _service;

        public UserController(IUserService service)
        {
            _service = service;
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
        [HttpGet("me")]
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
            return Ok(users.Select(u => new {
                u.Id,
                u.NomeCompleto,
                u.Email,
                u.Perfil
            }));
        }

    }
}
