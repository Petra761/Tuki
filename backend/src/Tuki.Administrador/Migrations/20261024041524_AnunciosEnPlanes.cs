using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tuki.Administrador.Migrations
{
    /// <inheritdoc />
    public partial class AnunciosEnPlanes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "removes_ads",
                table: "plans",
                type: "boolean",
                nullable: false,
                defaultValue: false
            );

            // Los tres planes de pago dejan de mostrar anuncios; Gratis conserva
            // el false que acaba de traer la columna nueva
            migrationBuilder.Sql(
                """
                UPDATE plans
                SET removes_ads = true
                WHERE name IN ('Compra única', 'Premium mensual', 'Premium anual');
                """
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "removes_ads", table: "plans");
        }
    }
}
