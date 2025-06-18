using MarcacoesOnline.Model;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;

public class PdfService
{
    public byte[] GerarPdfParaPedido(PedidoMarcacao pedido)
    {
        var corPrimaria = Colors.Blue.Medium;

        var documento = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(30);
                page.Size(PageSizes.A4);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(12));

                page.Header().Row(row =>
                {
                    row.RelativeColumn().Text("📄 Detalhes da Marcação")
                        .FontSize(20).Bold().FontColor(corPrimaria);

                    row.ConstantColumn(100).AlignRight().Text($"Pedido #{pedido.Id}")
                        .FontSize(14).SemiBold();
                });

                page.Content().PaddingVertical(10).Column(col =>
                {
                    col.Item().PaddingBottom(5).Text("📌 Informação Geral").Bold().FontSize(14);

                    col.Item().Row(row =>
                    {
                        row.RelativeColumn().Column(col1 =>
                        {
                            col1.Item().Text($"Estado: {pedido.Estado}");
                            col1.Item().Text($"Horário: {pedido.HorarioPreferido}");
                            col1.Item().Text($"Início Preferido: {pedido.DataInicioPreferida:dd/MM/yyyy}");
                        });

                        row.RelativeColumn().Column(col2 =>
                        {
                            col2.Item().Text($"Fim Preferido: {pedido.DataFimPreferida:dd/MM/yyyy}");
                            col2.Item().Text($"Data Agendada: {(pedido.DataAgendada.HasValue ? pedido.DataAgendada.Value.ToString("dd/MM/yyyy") : "Ainda não agendado")}");
                        });
                    });

                    col.Item().PaddingTop(10).Text("🧍‍♀️ Dados do Utente").Bold().FontSize(14);
                    col.Item().Text($"Nome: {pedido.User?.NomeCompleto ?? "N/A"}");
                    col.Item().Text($"Email: {pedido.User?.Email ?? "-"}");
                    col.Item().Text($"Telemóvel: {pedido.User?.Telemovel ?? "-"}");

                    if (!string.IsNullOrWhiteSpace(pedido.Observacoes))
                    {
                        col.Item().PaddingTop(10).Text("🗒 Observações").Bold().FontSize(14);
                        col.Item().Text(pedido.Observacoes);
                    }

                    col.Item().PaddingTop(15).Text("🧾 Atos Clínicos").Bold().FontSize(14);

                    if (pedido.ActosClinicos?.Any() == true)
                    {
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(cols =>
                            {
                                cols.RelativeColumn();
                                cols.RelativeColumn();
                                cols.RelativeColumn();
                            });

                            table.Header(header =>
                            {
                                header.Cell().Text("Tipo").Bold();
                                header.Cell().Text("Subsistema").Bold();
                                header.Cell().Text("Profissional").Bold();
                            });

                            foreach (var a in pedido.ActosClinicos)
                            {
                                table.Cell().Text(a.Tipo);
                                table.Cell().Text(a.SubsistemaSaude);
                                table.Cell().Text(a.Profissional);
                            }
                        });
                    }
                    else
                    {
                        col.Item().Text("Sem atos clínicos registrados.");
                    }
                });

                page.Footer().AlignRight().Text($"📅 Gerado em {DateTime.Now:dd/MM/yyyy HH:mm}")
                    .FontSize(10).FontColor(Colors.Grey.Medium);
            });
        });

        return documento.GeneratePdf();
    }
}
