using MarcacoesOnline.DTO;
using MarcacoesOnline.Model;
using Microsoft.AspNetCore.Mvc;
using MarcacoesOnline.Interfaces;
using MarcacoesOnline.Interfaces.Services;
using System;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace MarcacoesOnlineApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PedidoMarcacaoController : ControllerBase
{
    private readonly IPedidoMarcacaoService _service;
    private readonly IUserRepository _userRepo;
    private readonly IPedidoMarcacaoRepository _pedidoRepo;
    private readonly IEmailService _emailService;
    private readonly IPedidoMarcacaoRepository _marcacaoRepo;

    public PedidoMarcacaoController(
    IPedidoMarcacaoService service,
    IUserRepository userRepo,
    IPedidoMarcacaoRepository pedidoRepo,
    IEmailService emailService,
    IPedidoMarcacaoRepository marcacaoRepo)
    {
        _service = service;
        _userRepo = userRepo;
        _pedidoRepo = pedidoRepo;
        _emailService = emailService;
        _marcacaoRepo = marcacaoRepo;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<PedidoMarcacao>>> GetAll()
        => Ok(await _service.GetAllAsync());

    [HttpGet("{id}")]
    public async Task<ActionResult<PedidoMarcacao>> Get(int id)
    {
        var pedido = await _service.GetByIdAsync(id);
        if (pedido == null) return NotFound();
        return Ok(pedido);
    }

    [Authorize(Roles = "Registado,Anonimo")]
    [HttpPost]
    public async Task<ActionResult<PedidoMarcacao>> Create(PedidoMarcacao pedido)
    {
        var userIdClaim = User.Claims.FirstOrDefault(c => c.Type.EndsWith("nameidentifier"));
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            return Unauthorized("Utilizador não autenticado.");

        pedido.UserId = userId;

        var novo = await _service.CreateAsync(pedido);
        return CreatedAtAction(nameof(Get), new { id = novo.Id }, novo);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, PedidoMarcacao pedido)
    {
        var atualizado = await _service.UpdateAsync(id, pedido);
        if (!atualizado) return NotFound();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var removido = await _service.DeleteAsync(id);
        if (!removido) return NotFound();
        return NoContent();
    }

    [HttpPost("anonimo")]
    public async Task<IActionResult> CriarPedidoAnonimo([FromBody] PedidoAnonimoDto dto)
    {
        // 1. Validar campos obrigatórios
        if (string.IsNullOrWhiteSpace(dto.NomeCompleto))
            return BadRequest("O nome completo é obrigatório.");

        if (string.IsNullOrWhiteSpace(dto.Email))
            return BadRequest("O e-mail é obrigatório.");

        if (string.IsNullOrWhiteSpace(dto.Morada))
            return BadRequest("A morada é obrigatória.");

        if (dto.DataNascimento == default)
            return BadRequest("A data de nascimento é obrigatória.");

        var idade = DateTime.Today.Year - dto.DataNascimento.Year;
        if (dto.DataNascimento.Date > DateTime.Today.AddYears(-idade)) idade--;
        if (idade < 18)
            return BadRequest("O utilizador deve ter pelo menos 18 anos.");

        // 2. Verificar duplicidade de e-mail
        if (await _userRepo.ExistsByEmailAsync(dto.Email, 0))
            return BadRequest("Este e-mail já está em uso.");

        // 3. Limitar o número de contas por telemóvel (até 3)
        if (!string.IsNullOrWhiteSpace(dto.Telemovel))
        {
            var total = await _userRepo.CountByTelemovelAsync(dto.Telemovel, 0);
            if (total >= 3)
                return BadRequest("Este número de telemóvel já está associado a 3 contas.");
        }

        // 4. Criar o utente anónimo
        var user = new User
        {
            NumeroUtente = dto.NumeroUtente,
            NomeCompleto = dto.NomeCompleto,
            DataNascimento = dto.DataNascimento,
            Genero = dto.Genero,
            Telemovel = dto.Telemovel,
            Email = dto.Email,
            Morada = dto.Morada,
            Perfil = Perfil.Anonimo,
            PasswordHash = string.Empty,
            FotoPath = string.Empty
        };

        await _userRepo.AddAsync(user);
        await _userRepo.SaveChangesAsync();

        // 5. Criar o pedido
        var pedido = new PedidoMarcacao
        {
            CodigoReferencia = "MAR" + Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper(),
            Estado = EstadoPedido.Pedido,
            UserId = user.Id,
            Observacoes = dto.Observacoes,
            DataInicioPreferida = dto.DataInicioPreferida,
            DataFimPreferida = dto.DataFimPreferida,
            HorarioPreferido = dto.HorarioPreferido,
            ActosClinicos = dto.ActosClinicos.Select(ac => new ActoClinico
            {
                Tipo = ac.Tipo,
                SubsistemaSaude = ac.SubsistemaSaude,
                Profissional = ac.Profissional
            }).ToList()
        };

        await _pedidoRepo.AddAsync(pedido);
        await _pedidoRepo.SaveChangesAsync();

        return CreatedAtAction(nameof(Get), new { id = pedido.Id }, pedido);
    }

    [Authorize]
    [HttpGet("protegido")]
    public IActionResult SomenteParaLogados()
    {
        return Ok("Acesso autorizado com token JWT!");
    }

    [Authorize(Roles = "Administrativo")]
    [HttpGet("admin/pedidos")]
    public async Task<IActionResult> GetTodosPedidos()
    {
        var pedidos = await _service.GetAllAsync();
        return Ok(pedidos);
    }

    [Authorize(Roles = "Administrativo")]
    [HttpPatch("admin/agendar/{id}")]
    public async Task<IActionResult> AgendarPedido(int id, [FromBody] DateTime dataAgendada)
    {
        // Buscar pedido
        var pedido = await _service.GetByIdAsync(id);
        if (pedido == null)
            return NotFound("Pedido de marcação não encontrado.");

        // Verifica se já está agendado
        if (pedido.Estado == EstadoPedido.Agendado || pedido.DataAgendada.HasValue)
            return BadRequest("Este pedido já foi agendado.");

        // Verifica intervalo de preferência
        if (dataAgendada < pedido.DataInicioPreferida || dataAgendada > pedido.DataFimPreferida)
            return BadRequest("A data agendada está fora do intervalo preferido pelo utente.");

        // Notificar por e-mail (se possível)
        if (!string.IsNullOrWhiteSpace(pedido.User?.Email))
        {
            var mensagem = $"""
            Olá {pedido.User.NomeCompleto},

            A sua marcação foi confirmada com sucesso.

            📅 Data Agendada: {pedido.DataInicioPreferida}
            🕐 Período Preferido: {pedido.HorarioPreferido}
            📌 Observações: {pedido.Observacoes}
            📋 Código de Referência: {pedido.CodigoReferencia}

            Pode acompanhar suas marcações no sistema.

            Obrigado,
            Equipa de Atendimento
            """;

            await _emailService.EnviarConfirmacaoAsync(
                pedido.User.Email,
                "Confirmação de Marcação",
                mensagem
            );
        }


        // Atualiza estado e data
        pedido.Estado = EstadoPedido.Agendado;
        pedido.DataAgendada = dataAgendada;

        await _service.UpdateAsync(id, pedido);

        return Ok(new
        {
            message = "Pedido agendado com sucesso.",
            pedido.Id,
            novoEstado = pedido.Estado.ToString(),
            pedido.DataAgendada
        });
    }

    [HttpGet("admin/pedidos/estado-nome/{estadoNome}")]
    public async Task<IActionResult> GetPedidosPorEstadoNome(string estadoNome)
    {
        if (!Enum.TryParse<EstadoPedido>(estadoNome, true, out var estado))
            return BadRequest("Estado inválido.");

        var pedidos = await _service.GetAllAsync();
        var filtrados = pedidos.Where(p => p.Estado == estado);

        return Ok(filtrados);
    }

    [Authorize(Roles = "Administrativo")]
    [HttpPatch("admin/realizar/{id}")]
    public async Task<IActionResult> MarcarComoRealizadoAsync(int id)
    {
        var pedido = await _pedidoRepo.GetByIdAsync(id);
        if (pedido == null)
            return NotFound(new { sucesso = false, erro = "Marcação não encontrada." });

        if (pedido.Estado != EstadoPedido.Agendado)
            return BadRequest(new { sucesso = false, erro = "Só pode marcar como realizado se estiver no estado 'Agendado'." });

        if (!pedido.DataAgendada.HasValue)
            return BadRequest(new { sucesso = false, erro = "A marcação não possui data agendada." });

        /*if (DateTime.Now < pedido.DataAgendada.Value)
            return BadRequest(new { sucesso = false, erro = "A marcação ainda não ocorreu. Só pode ser marcada como realizada após a data agendada." });*/

        pedido.Estado = EstadoPedido.Realizado;

        _pedidoRepo.Update(pedido);
        await _pedidoRepo.SaveChangesAsync();

        return Ok(new { sucesso = true });
    }

    [Authorize(Roles = "Registado,Anonimo")]
    [HttpGet("user/historico")]
    public async Task<IActionResult> GetHistoricoDoUtente()
    {
        // Obter o ID do utilizador autenticado a partir do token
        var userIdClaim = User.Claims.FirstOrDefault(c => c.Type.EndsWith("nameidentifier"));
        if (userIdClaim == null)
            return Unauthorized();

        if (!int.TryParse(userIdClaim.Value, out var userId))
            return BadRequest("ID inválido no token.");

        // Obter todos os pedidos e filtrar pelo ID do utilizador
        var pedidos = await _service.GetAllAsync();
        var historico = pedidos
            .Where(p => p.UserId == userId)
            .Select(p => new
            {
                p.Id,
                p.Estado,
                p.DataAgendada,
                p.DataInicioPreferida,
                p.DataFimPreferida,
                p.HorarioPreferido,
                p.Observacoes,
                Actos = p.ActosClinicos.Select(a => new
                {
                    a.Tipo,
                    a.SubsistemaSaude,
                    a.Profissional
                })
            });

        return Ok(historico);
    }

    [Authorize(Roles = "Registado,Administrativo,Administrador")]
    [HttpGet("{id}/pdf")]
    public async Task<IActionResult> ExportarPedidoParaPdf(int id, [FromServices] PdfService pdfService)
    {
        var pedido = await _service.GetByIdAsync(id);
        if (pedido == null)
            return NotFound("Pedido não encontrado.");

        var pdfBytes = pdfService.GerarPdfParaPedido(pedido);
        return File(pdfBytes, "application/pdf", $"pedido_{id}.pdf");
    }

    [HttpGet("admin/pedidos/filtros")]
    public async Task<IActionResult> GetPedidosComFiltros(
    [FromQuery] string? estado,
    [FromQuery] string? dataInicio,
    [FromQuery] string? dataFim)
    {
        var pedidos = await _service.GetAllAsync();

        if (!string.IsNullOrEmpty(estado))
            pedidos = pedidos.Where(p => p.Estado.Equals(estado));

        if (DateTime.TryParse(dataInicio, out var inicioDate))
            pedidos = pedidos.Where(p => p.DataInicioPreferida >= inicioDate);

        if (DateTime.TryParse(dataFim, out var fimDate))
            pedidos = pedidos.Where(p => p.DataFimPreferida <= fimDate);

        return Ok(pedidos);
    }

    [AllowAnonymous]
    [HttpGet("consulta-marcacao/{codigoReferencia}")]
    public async Task<IActionResult> ConsultarMarcacaoPorCodigo(string codigoReferencia)
    {
        var marcacao = await _marcacaoRepo.GetByCodigoReferenciaAsync(codigoReferencia);
        if (marcacao == null)
            return NotFound(new { message = "Marcação não encontrada." });

        return Ok(new
        {
            marcacao.Id,
            marcacao.Estado,
            marcacao.DataInicioPreferida,
            marcacao.DataFimPreferida,
            marcacao.HorarioPreferido,
            marcacao.Observacoes,
            Utente = marcacao.User == null ? null : new
            {
                marcacao.User.Id,
                marcacao.User.NomeCompleto,
                marcacao.User.Email,
                marcacao.User.Telemovel,
                marcacao.User.Morada,
                marcacao.User.DataNascimento,
                marcacao.User.Genero
            },
            ActosClinicos = marcacao.ActosClinicos.Select(a => new {
                a.Id,
                a.Tipo,
                a.SubsistemaSaude,
                a.Profissional
            })
        });
    }

    // 2. Cancelamento de Marcação por Código
    [AllowAnonymous]
    [HttpPatch("consulta-marcacao/{codigoReferencia}/cancelar")]
    public async Task<IActionResult> CancelarMarcacaoPorCodigo(string codigoReferencia)
    {
        var marcacao = await _marcacaoRepo.GetByCodigoReferenciaAsync(codigoReferencia);
        if (marcacao == null)
            return NotFound(new { message = "Marcação não encontrada." });

        if (marcacao.Estado == EstadoPedido.Cancelado)
            return BadRequest(new { message = "A marcação já está cancelada." });

        marcacao.Estado = EstadoPedido.Cancelado;
        await _marcacaoRepo.SaveChangesAsync();

        return Ok(new { message = "Marcação cancelada com sucesso." });
    }

}
