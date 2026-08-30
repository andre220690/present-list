using System;
using BirthdayGifts.Api.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BirthdayGifts.Api.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260830081000_InitialCreate")]
public partial class InitialCreate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "admin_users",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                username = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                password_hash = table.Column<string>(type: "text", nullable: false),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table => table.PrimaryKey("pk_admin_users", x => x.id));

        migrationBuilder.CreateTable(
            name: "gifts",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                product_url = table.Column<string>(type: "text", nullable: false),
                image_path = table.Column<string>(type: "text", nullable: false),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table => table.PrimaryKey("pk_gifts", x => x.id));

        migrationBuilder.CreateTable(
            name: "reservations",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                gift_id = table.Column<Guid>(type: "uuid", nullable: false),
                reserved_by_name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                visitor_token_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                cancelled_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_reservations", x => x.id);
                table.ForeignKey(
                    name: "fk_reservations_gifts_gift_id",
                    column: x => x.gift_id,
                    principalTable: "gifts",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "ix_admin_users_username",
            table: "admin_users",
            column: "username",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ux_reservations_active_gift",
            table: "reservations",
            column: "gift_id",
            unique: true,
            filter: "cancelled_at IS NULL");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "admin_users");
        migrationBuilder.DropTable(name: "reservations");
        migrationBuilder.DropTable(name: "gifts");
    }
}
