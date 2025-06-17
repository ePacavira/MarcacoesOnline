using MarcacoesOnline.DTO;
using MarcacoesOnline.Model;
using Microsoft.AspNetCore.Mvc;
using MarcacoesOnline.Interfaces;
using MarcacoesOnline.Interfaces.Services;
using System;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Authorization;

namespace MarcacoesOnlineApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PedidoMarcacaoController : ControllerBase
{
    private readonly IPedidoMarcacaoService _service;
    private readonly IUserRepository _userRepo;
    private readonly IPedidoMarcacaoRepository _pedidoRepo;
    private readonly IEmailService _emailService;

    public PedidoMarcacaoController(
    IPedidoMarcacaoService service,
    IUserRepository userRepo,
    IPedidoMarcacaoRepository pedidoRepo,
    IEmailService emailService)
    {
        _service = service;
        _userRepo = userRepo;
        _pedidoRepo = pedidoRepo;
        _emailService = emailService;
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

    [HttpPost]
    public async Task<ActionResult<PedidoMarcacao>> Create(PedidoMarcacao pedido)
    {
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
            PasswordHash = string.Empty
        };
        await _userRepo.AddAsync(user);
        await _userRepo.SaveChangesAsync();

        var pedido = new PedidoMarcacao
        {
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
    public async Task<IActionResult> AgendarPedido(int id)
    {
        var pedido = await _service.GetByIdAsync(id);
        if (pedido == null) return NotFound();

        pedido.Estado = EstadoPedido.Agendado;

        await _service.UpdateAsync(id, pedido);

        var user = pedido.User;
        if (user != null && !string.IsNullOrEmpty(user.Email))
        {
            await _emailService.EnviarConfirmacaoAsync(
                user.Email,
                "Marcação Agendada",
                $"A sua marcação foi agendada para o intervalo {pedido.DataInicioPreferida:dd/MM/yyyy} - {pedido.DataFimPreferida:dd/MM/yyyy}.\nHorário: {pedido.HorarioPreferido}"
            );
        }
        return NoContent();
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
    public async Task<IActionResult> MarcarComoRealizado(int id)
    {
        var pedido = await _service.GetByIdAsync(id);
        if (pedido == null)
            return NotFound("Pedido não encontrado.");

        if (pedido.Estado != EstadoPedido.Agendado)
            return BadRequest("Só é possível marcar como Realizado um pedido que esteja Agendado.");

        pedido.Estado = EstadoPedido.Realizado;
        await _service.UpdateAsync(id, pedido);

        return Ok(new
        {
            mensagem = "Pedido marcado como Realizado com sucesso.",
            pedido.Id,
            novoEstado = pedido.Estado.ToString()
        });
    }

}
