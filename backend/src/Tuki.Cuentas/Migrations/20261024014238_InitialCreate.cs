using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Tuki.Cuentas.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "account_types",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(
                        type: "character varying(100)",
                        maxLength: 100,
                        nullable: false
                    ),
                    is_person = table.Column<bool>(type: "boolean", nullable: false),
                    is_archived = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    updated_at = table.Column<DateTimeOffset>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_account_types", x => x.id);
                }
            );

            migrationBuilder.CreateTable(
                name: "categories",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    parent_id = table.Column<Guid>(type: "uuid", nullable: true),
                    name = table.Column<string>(
                        type: "character varying(100)",
                        maxLength: 100,
                        nullable: false
                    ),
                    kind = table.Column<string>(
                        type: "character varying(20)",
                        maxLength: 20,
                        nullable: false
                    ),
                    is_archived = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    updated_at = table.Column<DateTimeOffset>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_categories", x => x.id);
                    table.ForeignKey(
                        name: "fk_categories_categories_parent_id",
                        column: x => x.parent_id,
                        principalTable: "categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "accounts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(
                        type: "character varying(100)",
                        maxLength: 100,
                        nullable: false
                    ),
                    currency_code = table.Column<string>(
                        type: "character varying(3)",
                        maxLength: 3,
                        nullable: false
                    ),
                    is_default = table.Column<bool>(type: "boolean", nullable: false),
                    is_archived = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    updated_at = table.Column<DateTimeOffset>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_accounts", x => x.id);
                    table.ForeignKey(
                        name: "fk_accounts_account_types_account_type_id",
                        column: x => x.account_type_id,
                        principalTable: "account_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict
                    );
                }
            );

            migrationBuilder.InsertData(
                table: "account_types",
                columns: new[]
                {
                    "id",
                    "created_at",
                    "is_archived",
                    "is_person",
                    "name",
                    "updated_at",
                },
                values: new object[,]
                {
                    {
                        new Guid("00000000-0000-0000-0000-000000000001"),
                        new DateTimeOffset(
                            new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                            new TimeSpan(0, 0, 0, 0, 0)
                        ),
                        false,
                        false,
                        "Efectivo",
                        new DateTimeOffset(
                            new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                            new TimeSpan(0, 0, 0, 0, 0)
                        ),
                    },
                    {
                        new Guid("00000000-0000-0000-0000-000000000002"),
                        new DateTimeOffset(
                            new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                            new TimeSpan(0, 0, 0, 0, 0)
                        ),
                        false,
                        false,
                        "Tarjeta bancaria",
                        new DateTimeOffset(
                            new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                            new TimeSpan(0, 0, 0, 0, 0)
                        ),
                    },
                    {
                        new Guid("00000000-0000-0000-0000-000000000003"),
                        new DateTimeOffset(
                            new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                            new TimeSpan(0, 0, 0, 0, 0)
                        ),
                        false,
                        false,
                        "Cuenta digital",
                        new DateTimeOffset(
                            new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                            new TimeSpan(0, 0, 0, 0, 0)
                        ),
                    },
                    {
                        new Guid("00000000-0000-0000-0000-000000000004"),
                        new DateTimeOffset(
                            new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                            new TimeSpan(0, 0, 0, 0, 0)
                        ),
                        false,
                        false,
                        "Otro",
                        new DateTimeOffset(
                            new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                            new TimeSpan(0, 0, 0, 0, 0)
                        ),
                    },
                    {
                        new Guid("00000000-0000-0000-0000-000000000005"),
                        new DateTimeOffset(
                            new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                            new TimeSpan(0, 0, 0, 0, 0)
                        ),
                        false,
                        true,
                        "Persona",
                        new DateTimeOffset(
                            new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                            new TimeSpan(0, 0, 0, 0, 0)
                        ),
                    },
                }
            );

            migrationBuilder.InsertData(
                table: "categories",
                columns: new[]
                {
                    "id",
                    "created_at",
                    "is_archived",
                    "kind",
                    "name",
                    "parent_id",
                    "updated_at",
                },
                values: new object[,]
                {
                    {
                        new Guid("00000000-0000-0000-0000-000000000101"),
                        new DateTimeOffset(
                            new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                            new TimeSpan(0, 0, 0, 0, 0)
                        ),
                        false,
                        "Expense",
                        "Alimentación",
                        null,
                        new DateTimeOffset(
                            new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                            new TimeSpan(0, 0, 0, 0, 0)
                        ),
                    },
                    {
                        new Guid("00000000-0000-0000-0000-000000000102"),
                        new DateTimeOffset(
                            new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                            new TimeSpan(0, 0, 0, 0, 0)
                        ),
                        false,
                        "Expense",
                        "Transporte",
                        null,
                        new DateTimeOffset(
                            new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                            new TimeSpan(0, 0, 0, 0, 0)
                        ),
                    },
                    {
                        new Guid("00000000-0000-0000-0000-000000000103"),
                        new DateTimeOffset(
                            new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                            new TimeSpan(0, 0, 0, 0, 0)
                        ),
                        false,
                        "Expense",
                        "Educación",
                        null,
                        new DateTimeOffset(
                            new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                            new TimeSpan(0, 0, 0, 0, 0)
                        ),
                    },
                    {
                        new Guid("00000000-0000-0000-0000-000000000104"),
                        new DateTimeOffset(
                            new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                            new TimeSpan(0, 0, 0, 0, 0)
                        ),
                        false,
                        "Expense",
                        "Salud",
                        null,
                        new DateTimeOffset(
                            new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                            new TimeSpan(0, 0, 0, 0, 0)
                        ),
                    },
                    {
                        new Guid("00000000-0000-0000-0000-000000000105"),
                        new DateTimeOffset(
                            new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                            new TimeSpan(0, 0, 0, 0, 0)
                        ),
                        false,
                        "Expense",
                        "Hogar",
                        null,
                        new DateTimeOffset(
                            new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                            new TimeSpan(0, 0, 0, 0, 0)
                        ),
                    },
                    {
                        new Guid("00000000-0000-0000-0000-000000000106"),
                        new DateTimeOffset(
                            new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                            new TimeSpan(0, 0, 0, 0, 0)
                        ),
                        false,
                        "Expense",
                        "Ocio",
                        null,
                        new DateTimeOffset(
                            new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                            new TimeSpan(0, 0, 0, 0, 0)
                        ),
                    },
                    {
                        new Guid("00000000-0000-0000-0000-000000000107"),
                        new DateTimeOffset(
                            new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                            new TimeSpan(0, 0, 0, 0, 0)
                        ),
                        false,
                        "Expense",
                        "Otros gastos",
                        null,
                        new DateTimeOffset(
                            new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                            new TimeSpan(0, 0, 0, 0, 0)
                        ),
                    },
                    {
                        new Guid("00000000-0000-0000-0000-000000000108"),
                        new DateTimeOffset(
                            new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                            new TimeSpan(0, 0, 0, 0, 0)
                        ),
                        false,
                        "Income",
                        "Sueldo",
                        null,
                        new DateTimeOffset(
                            new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                            new TimeSpan(0, 0, 0, 0, 0)
                        ),
                    },
                    {
                        new Guid("00000000-0000-0000-0000-000000000109"),
                        new DateTimeOffset(
                            new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                            new TimeSpan(0, 0, 0, 0, 0)
                        ),
                        false,
                        "Income",
                        "Otros ingresos",
                        null,
                        new DateTimeOffset(
                            new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                            new TimeSpan(0, 0, 0, 0, 0)
                        ),
                    },
                }
            );

            migrationBuilder.InsertData(
                table: "accounts",
                columns: new[]
                {
                    "id",
                    "account_type_id",
                    "created_at",
                    "currency_code",
                    "is_archived",
                    "is_default",
                    "name",
                    "updated_at",
                },
                values: new object[]
                {
                    new Guid("00000000-0000-0000-0000-0000000000a1"),
                    new Guid("00000000-0000-0000-0000-000000000001"),
                    new DateTimeOffset(
                        new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                        new TimeSpan(0, 0, 0, 0, 0)
                    ),
                    "BOB",
                    false,
                    true,
                    "Efectivo",
                    new DateTimeOffset(
                        new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                        new TimeSpan(0, 0, 0, 0, 0)
                    ),
                }
            );

            migrationBuilder.CreateIndex(
                name: "ix_account_types_name",
                table: "account_types",
                column: "name",
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "ix_accounts_account_type_id",
                table: "accounts",
                column: "account_type_id"
            );

            migrationBuilder.CreateIndex(
                name: "ix_accounts_is_archived",
                table: "accounts",
                column: "is_archived"
            );

            migrationBuilder.CreateIndex(
                name: "ix_accounts_is_default",
                table: "accounts",
                column: "is_default",
                unique: true,
                filter: "is_default = true"
            );

            migrationBuilder.CreateIndex(
                name: "ix_categories_kind_is_archived",
                table: "categories",
                columns: new[] { "kind", "is_archived" }
            );

            migrationBuilder.CreateIndex(
                name: "ix_categories_parent_id",
                table: "categories",
                column: "parent_id"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "accounts");

            migrationBuilder.DropTable(name: "categories");

            migrationBuilder.DropTable(name: "account_types");
        }
    }
}
