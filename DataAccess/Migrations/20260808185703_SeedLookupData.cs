using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class SeedLookupData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Departments",
                columns: new[] { "Id", "Code", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "IsDeleted", "Name", "ParentDepartmentId", "UpdatedAt", "UpdatedBy" },
                values: new object[] { new Guid("d0000000-0000-0000-0000-000000000001"), "IT", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, false, "IT Support", null, null, null });

            migrationBuilder.InsertData(
                table: "TicketPriorities",
                columns: new[] { "Id", "Code", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "IsDeleted", "SortOrder", "UpdatedAt", "UpdatedBy" },
                values: new object[] { new Guid("a0000000-0000-0000-0000-000000000002"), "Normal", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, false, new Guid("00000000-0000-0000-0000-000000000001"), null, null });

            migrationBuilder.InsertData(
                table: "TicketStatuses",
                columns: new[] { "Id", "Code", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "IsDeleted", "IsTerminal", "UpdatedAt", "UpdatedBy" },
                values: new object[] { new Guid("a0000000-0000-0000-0000-000000000003"), "Open", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, false, false, null, null });

            migrationBuilder.InsertData(
                table: "Categories",
                columns: new[] { "Id", "CreatedAt", "CreatedBy", "DefaultPriorityId", "DeletedAt", "DeletedBy", "DepartmentId", "IsDeleted", "Name", "UpdatedAt", "UpdatedBy" },
                values: new object[] { new Guid("c0000000-0000-0000-0000-000000000001"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, new Guid("a0000000-0000-0000-0000-000000000002"), null, null, new Guid("d0000000-0000-0000-0000-000000000001"), false, "General", null, null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("c0000000-0000-0000-0000-000000000001"));

            migrationBuilder.DeleteData(
                table: "TicketStatuses",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000003"));

            migrationBuilder.DeleteData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: new Guid("d0000000-0000-0000-0000-000000000001"));

            migrationBuilder.DeleteData(
                table: "TicketPriorities",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000002"));
        }
    }
}
