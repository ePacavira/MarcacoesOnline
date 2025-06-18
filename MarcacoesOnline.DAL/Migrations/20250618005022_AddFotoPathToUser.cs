using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MarcacoesOnline.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddFotoPathToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FotoPath",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FotoPath",
                table: "Users");
        }
    }
}
