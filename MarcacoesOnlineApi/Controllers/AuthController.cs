using Microsoft.AspNetCore.Mvc;
using MarcacoesOnline.DTO;
using MarcacoesOnline.Interfaces.Services;
using MarcacoesOnline.Services;
using MarcacoesOnline.DAL;
using Microsoft.EntityFrameworkCore;

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
        var user = await _userService.GetByEmailAsync(dto.Email);
        if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
        {
            return Unauthorized("Credenciais inválidas ou utilizador não registado.");
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
                user.Perfil
            }
        });
    }
}
