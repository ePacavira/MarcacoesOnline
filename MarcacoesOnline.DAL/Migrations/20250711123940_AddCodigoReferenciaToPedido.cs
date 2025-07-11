using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MarcacoesOnline.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddCodigoReferenciaToPedido : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CodigoReferencia",
                table: "PedidosMarcacao",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CodigoReferencia",
                table: "PedidosMarcacao");
        }
    }
}
