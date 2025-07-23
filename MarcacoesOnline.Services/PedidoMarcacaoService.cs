using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MarcacoesOnline.DAL.Repositories;
using MarcacoesOnline.Interfaces;
using MarcacoesOnline.Model;

public class PedidoMarcacaoService : IPedidoMarcacaoService
{
    private readonly IPedidoMarcacaoRepository _pedidoRepo;
    private readonly IUserRepository _userRepo;

    public PedidoMarcacaoService(IPedidoMarcacaoRepository pedidoRepo, IUserRepository userRepo)
    {
        _pedidoRepo = pedidoRepo;
        _userRepo = userRepo;
    }

    public async Task<IEnumerable<PedidoMarcacao>> GetAllAsync()
        => await _pedidoRepo.GetAllAsync();

    public async Task<PedidoMarcacao?> GetByIdAsync(int id)
        => await _pedidoRepo.GetByIdAsync(id);

    public async Task<PedidoMarcacao> CreateAsync(PedidoMarcacao pedido)
    {
        // 1. Validar: Data Início ≤ Data Fim
        if (pedido.DataInicioPreferida > pedido.DataFimPreferida)
            throw new Exception("A data de início preferida deve ser anterior ou igual à data de fim preferida.");

        // 2. Validar: Estado inicial deve ser Pedido
        if (pedido.Estado != EstadoPedido.Pedido)
                    throw new Exception("O estado inicial da marcação deve ser 'Pedido'.");

        // 3. Validar: Apenas utentes registados (UserId obrigatório)
        if (pedido.UserId == null || pedido.UserId == 0)
            throw new Exception("A marcação deve estar associada a um utilizador.");

        var user = await _userRepo.GetByIdAsync(pedido.UserId);
        if (user == null || user.Perfil == Perfil.Anonimo) // Caso o anónimo seja tratado à parte
            throw new Exception("Utilizador não registado ou não autorizado.");

        // 4. Validar: Pelo menos um Ato Clínico
        if (pedido.ActosClinicos == null || !pedido.ActosClinicos.Any())
            throw new Exception("Deve adicionar pelo menos um ato clínico.");

        // 5. Validar: Horário preferido → verificar coerência
        if (pedido.DataInicioPreferida.HasValue && pedido.HorarioPreferido == "Manhã" && pedido.DataInicioPreferida.Value.Hour > 12)
            throw new Exception("Horário 'Manhã' requer hora de início antes do meio-dia.");

        if (pedido.DataFimPreferida.HasValue && pedido.HorarioPreferido == "Tarde" && pedido.DataFimPreferida.Value.Hour < 12)
            throw new Exception("Horário 'Tarde' requer hora de fim após o meio-dia.");

        // 6. Validar: Não pode haver marcação duplicada no mesmo intervalo para o mesmo utente
        var conflitos = await _pedidoRepo.ExisteMarcacaoNoIntervalo(
            pedido.UserId,
            pedido.DataInicioPreferida,
            pedido.DataFimPreferida
        );

        if (conflitos)
            throw new Exception("Já existe uma marcação para este utilizador nesse intervalo de datas.");

        // 7. Validar: O profissional não pode ter outra marcação agendada no mesmo intervalo
        foreach (var ato in pedido.ActosClinicos)
        {
            var conflitoProf = await _pedidoRepo.ExisteMarcacaoParaProfissionalNoIntervalo(
                ato.Profissional,
                pedido.DataInicioPreferida,
                pedido.DataFimPreferida
            );

            if (conflitoProf)
                throw new Exception($"O profissional {ato.Profissional} já tem uma marcação agendada nesse intervalo.");
        }

        // Tudo certo, cria
        await _pedidoRepo.AddAsync(pedido);
        await _pedidoRepo.SaveChangesAsync();

        return pedido;
    }

    public async Task<(bool Sucesso, string? Erro)> MarcarComoRealizadoAsync(int id)
    {
        var pedido = await _pedidoRepo.GetByIdAsync(id);
        if (pedido == null)
            return (false, "Marcação não encontrada.");

        if (pedido.Estado != EstadoPedido.Agendado)
            return (false, "Só pode marcar como realizado se estiver no estado 'Agendado'.");

        if (!pedido.DataAgendada.HasValue)
            return (false, "A marcação não possui data agendada.");

        if (DateTime.Now < pedido.DataAgendada.Value)
            return (false, "A marcação ainda não ocorreu. Só pode ser marcada como realizada após a data agendada.");

        pedido.Estado = EstadoPedido.Realizado;

        _pedidoRepo.Update(pedido);
        await _pedidoRepo.SaveChangesAsync();

        return (true, null);
    }

    public async Task<bool> UpdateAsync(int id, PedidoMarcacao pedido)
    {
        var existing = await _pedidoRepo.GetByIdAsync(id);
        if (existing == null) return false;

        // Atualiza os campos necessários
        existing.Estado = pedido.Estado;
        existing.DataAgendada = pedido.DataAgendada;
        existing.DataInicioPreferida = pedido.DataInicioPreferida;
        existing.DataFimPreferida = pedido.DataFimPreferida;
        existing.HorarioPreferido = pedido.HorarioPreferido;
        existing.Observacoes = pedido.Observacoes;

        _pedidoRepo.Update(existing);
        await _pedidoRepo.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var pedido = await _pedidoRepo.GetByIdAsync(id);
        if (pedido == null) return false;

        _pedidoRepo.Delete(pedido);
        await _pedidoRepo.SaveChangesAsync();
        return true;
    }
}

