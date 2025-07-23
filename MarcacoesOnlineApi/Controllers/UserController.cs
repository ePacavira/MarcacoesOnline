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
using Microsoft.Extensions.Hosting;

namespace MarcacoesOnlineApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly IUserService _service;
        private readonly IUserRepository _userRepo;
        private readonly IPedidoMarcacaoRepository _pedidoRepo;
        private readonly IWebHostEnvironment _environment;

        public UserController(IUserService service, IUserRepository userRepo, IPedidoMarcacaoRepository pedidoRepo, IWebHostEnvironment environment)
        {
            _service = service;
            _userRepo = userRepo;
            _pedidoRepo = pedidoRepo;
            _environment = environment;
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

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] User user)
        {
            var atualizado = await _service.UpdateAsync(id, user);
            if (!atualizado)
                return NotFound();

            return NoContent();
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
        [HttpPost("upload-foto")]
        public async Task<IActionResult> UploadFoto(IFormFile foto)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var user = await _service.GetByIdAsync(userId);
            if (user == null) return NotFound("Utilizador não encontrado.");

            if (foto == null || foto.Length == 0)
                return BadRequest("Nenhuma foto enviada.");

            // Validar tamanho (máx. 2MB)
            if (foto.Length > 2 * 1024 * 1024)
                return BadRequest("O tamanho máximo permitido é 2MB.");

            // Validar extensão e MIME
            var extensao = Path.GetExtension(foto.FileName).ToLowerInvariant();
            var mimeType = foto.ContentType.ToLowerInvariant();

            var extensoesPermitidas = new[] { ".jpg", ".jpeg", ".png" };
            var mimeTypesPermitidos = new[] { "image/jpeg", "image/png" };

            if (!extensoesPermitidas.Contains(extensao) || !mimeTypesPermitidos.Contains(mimeType))
                return BadRequest("Formato inválido. Apenas JPG ou PNG são permitidos.");

            // Criar nome seguro único
            var nomeFicheiro = $"foto_{userId}_{Guid.NewGuid()}{extensao}";
            var path = Path.Combine("wwwroot", "fotos", nomeFicheiro);

            // Garantir que a pasta existe
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);

            using (var stream = new FileStream(path, FileMode.Create))
            {
                await foto.CopyToAsync(stream);
            }

            // Atualizar caminho no banco
            user.FotoPath = $"/fotos/{nomeFicheiro}";
            await _service.UpdateAsync(userId, user);

            return Ok(new { mensagem = "Foto carregada com sucesso.", path = user.FotoPath });
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

        [Authorize(Roles = "Registado,Administrativo,Administrador")]
        [HttpGet("pedidos/{id}")]
        public async Task<IActionResult> GetPedidosDoUsuarioLogado()
        {
            // Extrai o ID do utilizador do JWT
            var userIdClaim = User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier");
            if (userIdClaim == null)
                return Unauthorized(new { message = "Token inválido." });

            if (!int.TryParse(userIdClaim.Value, out int userId))
                return Unauthorized(new { message = "ID de utilizador inválido." });

            var user = await _service.GetByIdAsync(userId);
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
        [HttpDelete("pedidos/{id}/cancelar")]
        public async Task<IActionResult> CancelarPedido(int id)
        {
            var pedido = await _pedidoRepo.GetByIdAsync(id);
            if (pedido == null)
                return NotFound();

            pedido.Estado = EstadoPedido.Cancelado;
            await _pedidoRepo.SaveChangesAsync();

            return Ok(new { message = "Pedido cancelado com sucesso." });
        }

        [AllowAnonymous]
        [HttpPost("contacto")]
        public async Task<IActionResult> EnviarMensagemContacto([FromBody] ContactMessageDto dto, [FromServices] IEmailService emailService)
        {
            var corpo = $@"
                <h3>Nova mensagem do formulário de contacto</h3>
                <p><strong>Nome:</strong> {dto.Nome}</p>
                <p><strong>Email:</strong> {dto.Email}</p>
                <p><strong>Telefone:</strong> {dto.Telefone}</p>
                <p><strong>Assunto:</strong> {dto.Assunto}</p>
                <p><strong>Mensagem:</strong><br>{dto.Mensagem}</p>
                <p><strong>Deseja receber newsletter?</strong> {(dto.Newsletter ? "Sim" : "Não")}</p>
                ";
            var destino = "conapro.044@marcacoes.com"; // ou buscar da config
            var assunto = $"📩 Nova mensagem de contacto: {dto.Assunto}";

            await emailService.EnviarConfirmacaoAsync(destino, assunto, corpo);

            return Ok(new { mensagem = "Mensagem enviada com sucesso!" });
        }

        [HttpGet("exists-email")]
        [AllowAnonymous]
        public async Task<IActionResult> VerificarEmailExiste([FromQuery] string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return BadRequest(new { exists = false });

            var existe = await _userRepo.ExistsByEmailAsync(email, 0);
            return Ok(new { exists = existe });
        }

        [HttpGet("exists-telemovel")]
        [AllowAnonymous]
        public async Task<IActionResult> VerificarTelemovelExiste([FromQuery] string telemovel)
        {
            if (string.IsNullOrWhiteSpace(telemovel))
                return BadRequest(new { exists = false });

            var existe = await _userRepo.CountByTelemovelAsync(telemovel, 0);
            return Ok(new { exists = existe >= 1 }); // true se houver pelo menos 1
        }
            

            // Upload de foto de perfil
            [HttpPost("{userId}/foto")]
            public async Task<IActionResult> UploadPhoto(int userId, IFormFile file)
            {
                try
                {
                    // Verificar se o utilizador existe
                    var user = await _service.GetUserByIdAsync(userId);
                    if (user == null)
                        return NotFound("Utilizador não encontrado");

                    // Verificar se o utilizador logado é o mesmo que está a fazer upload
                    var currentUserId = int.Parse(User.FindFirst("userId")?.Value);
                    if (currentUserId != userId)
                        return Forbid("Não tem permissão para alterar este perfil");

                    // Validar arquivo
                    if (file == null || file.Length == 0)
                        return BadRequest("Nenhum arquivo foi enviado");

                    // Validar tamanho (máx 2MB)
                    if (file.Length > 2 * 1024 * 1024)
                        return BadRequest("Arquivo muito grande. Máximo 2MB permitido");

                    // Validar tipo
                    var allowedTypes = new[] { "image/jpeg", "image/png", "image/gif" };
                    if (!allowedTypes.Contains(file.ContentType.ToLower()))
                        return BadRequest("Tipo de arquivo não suportado. Use JPG, PNG ou GIF");

                    // Criar diretório se não existir
                    var uploadsPath = Path.Combine(_environment.WebRootPath, "uploads", "photos");
                    if (!Directory.Exists(uploadsPath))
                        Directory.CreateDirectory(uploadsPath);

                    // Gerar nome único para o arquivo
                    var fileName = $"user_{userId}_{DateTime.Now:yyyyMMddHHmmss}{Path.GetExtension(file.FileName)}";
                    var filePath = Path.Combine(uploadsPath, fileName);

                    // Salvar arquivo
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }

                    // Atualizar caminho da foto no utilizador
                    var photoUrl = $"/uploads/photos/{fileName}";
                    await _service.UpdateUserPhotoAsync(userId, photoUrl);

                    return Ok(new { fotoPath = photoUrl });
                }
                catch (Exception ex)
                {
                    return StatusCode(500, $"Erro interno: {ex.Message}");
                }
            }

            // Remover foto de perfil
            [HttpDelete("{userId}/foto")]
            public async Task<IActionResult> RemovePhoto(int userId)
            {
                try
                {
                    var user = await _service.GetUserByIdAsync(userId);
                    if (user == null)
                        return NotFound("Utilizador não encontrado");

                    // Verificar permissões
                    var currentUserId = int.Parse(User.FindFirst("userId")?.Value);
                    if (currentUserId != userId)
                        return Forbid("Não tem permissão para alterar este perfil");

                    // Remover arquivo se existir
                    if (!string.IsNullOrEmpty(user.FotoPath))
                    {
                        var filePath = Path.Combine(_environment.WebRootPath, user.FotoPath.TrimStart('/'));
                        if (System.IO.File.Exists(filePath))
                        {
                            System.IO.File.Delete(filePath);
                        }
                    }

                    // Limpar caminho da foto no utilizador
                    await _service.UpdateUserPhotoAsync(userId, null);

                    return Ok(new { message = "Foto removida com sucesso" });
                }
                catch (Exception ex)
                {
                    return StatusCode(500, $"Erro interno: {ex.Message}");
                }
            }
    }
}