using Microsoft.AspNetCore.Mvc;
using MarcacoesOnline.DTO;
using MarcacoesOnline.Interfaces.Services;
using MarcacoesOnline.Services;
using MarcacoesOnline.DAL;
using Microsoft.EntityFrameworkCore;
using MarcacoesOnline.Model;
using Microsoft.AspNetCore.Components.Forms;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly JwtService _jwtService;
    private readonly MarcacoesOnlineDbContext _context;

    public AuthController(IUserService userService, JwtService jwtService, MarcacoesOnlineDbContext context)
    {
        _userService = userService;
        _jwtService = jwtService;
        _context = context;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        // Validação básica de entrada
        if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
        {
            return BadRequest(new { message = "E-mail e password são obrigatórios." });
        }

        var user = await _userService.GetByEmailAsync(dto.Email);

        // Verifica se o usuário existe e a senha está correta
        if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
        {
            return Unauthorized(new { message = "Credenciais inválidas." }); // erro genérico
        }

        // Verifica se o usuário está registado (ex: não é anônimo)
        if (user.Perfil == Perfil.Anonimo) // ou outra enum/status representando não registado
        {
            return Unauthorized(new { message = "Conta ainda não registada." });
        }

        var token = _jwtService.GenerateToken(user);

        return Ok(new
        {
            token,
            user = new
            {
                user.Id,
                user.NomeCompleto,
                user.Email,
                user.Perfil,
                user.Telemovel
            }
        });
    }
}
