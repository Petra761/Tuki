using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tuki.Administrador.Migrations
{
    /// <inheritdoc />
    public partial class PreciosEnCentavosYPlanesSinVencimiento : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Convierte el precio decimal a centavos multiplicando por 100 y redondeando,
            // preservando los importes existentes sin pérdida de datos
            migrationBuilder.Sql(
                "ALTER TABLE plans ALTER COLUMN price TYPE bigint USING ROUND(price * 100)::bigint;"
            );

            migrationBuilder.RenameColumn(name: "price", table: "plans", newName: "price_cents");

            migrationBuilder.AlterColumn<int>(
                name: "duration_days",
                table: "plans",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer"
            );

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "expires_at",
                table: "subscriptions",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone"
            );

            // Siembra y actualiza los cuatro planes oficiales manteniendo nombres únicos
            migrationBuilder.Sql(
                """
                INSERT INTO plans (id, name, price_cents, duration_days, is_available, created_at)
                VALUES
                    (gen_random_uuid(), 'Gratis', 0, NULL, true, now()),
                    (gen_random_uuid(), 'Compra única', 1990, NULL, true, now()),
                    (gen_random_uuid(), 'Premium mensual', 990, 30, true, now()),
                    (gen_random_uuid(), 'Premium anual', 7990, 365, true, now())
                ON CONFLICT (name) DO UPDATE SET
                    price_cents = EXCLUDED.price_cents,
                    duration_days = EXCLUDED.duration_days,
                    is_available = EXCLUDED.is_available;
                """
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(name: "price_cents", table: "plans", newName: "price");

            migrationBuilder.Sql(
                "ALTER TABLE plans ALTER COLUMN price TYPE numeric(18,2) USING (price / 100.0);"
            );

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "expires_at",
                table: "subscriptions",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(
                    new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                    new TimeSpan(0, 0, 0, 0, 0)
                ),
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldNullable: true
            );

            migrationBuilder.AlterColumn<int>(
                name: "duration_days",
                table: "plans",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true
            );
        }
    }
}
