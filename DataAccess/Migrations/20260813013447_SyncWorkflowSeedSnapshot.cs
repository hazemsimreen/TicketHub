using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class SyncWorkflowSeedSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "WorkflowDefinitions",
                columns: new[]
                {
                    "Id",
                    "CategoryId",
                    "CreatedAt",
                    "CreatedBy",
                    "DeletedAt",
                    "DeletedBy",
                    "DepartmentId",
                    "IsDefault",
                    "IsDeleted",
                    "Name",
                    "UpdatedAt",
                    "UpdatedBy",
                    "Version"
                },
                values: new object[,]
                {
                    {
                        new Guid("11111111-0000-0000-0000-000000000001"),
                        null,
                        new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                        null,
                        null,
                        null,
                        1,
                        true,
                        false,
                        "Default Workflow - Roads & Infrastructure",
                        null,
                        null,
                        1
                    },
                    {
                        new Guid("11111111-0000-0000-0000-000000000002"),
                        null,
                        new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                        null,
                        null,
                        null,
                        2,
                        true,
                        false,
                        "Default Workflow - Sanitation",
                        null,
                        null,
                        1
                    },
                    {
                        new Guid("11111111-0000-0000-0000-000000000003"),
                        null,
                        new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                        null,
                        null,
                        null,
                        3,
                        true,
                        false,
                        "Default Workflow - Street Lighting",
                        null,
                        null,
                        1
                    }
                });

            migrationBuilder.InsertData(
                table: "WorkflowSteps",
                columns: new[]
                {
                    "Id",
                    "AssignedUserId",
                    "CreatedAt",
                    "CreatedBy",
                    "DeletedAt",
                    "DeletedBy",
                    "IsDeleted",
                    "RoleId",
                    "StepOrder",
                    "UpdatedAt",
                    "UpdatedBy",
                    "WorkflowDefinitionId"
                },
                values: new object[,]
                {
                    {
                        new Guid("21111111-0000-0000-0000-000000000001"),
                        null,
                        new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                        null,
                        null,
                        null,
                        false,
                        2,
                        1,
                        null,
                        null,
                        new Guid("11111111-0000-0000-0000-000000000001")
                    },
                    {
                        new Guid("21111111-0000-0000-0000-000000000002"),
                        null,
                        new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                        null,
                        null,
                        null,
                        false,
                        3,
                        2,
                        null,
                        null,
                        new Guid("11111111-0000-0000-0000-000000000001")
                    },
                    {
                        new Guid("22222222-0000-0000-0000-000000000001"),
                        null,
                        new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                        null,
                        null,
                        null,
                        false,
                        2,
                        1,
                        null,
                        null,
                        new Guid("11111111-0000-0000-0000-000000000002")
                    },
                    {
                        new Guid("22222222-0000-0000-0000-000000000002"),
                        null,
                        new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                        null,
                        null,
                        null,
                        false,
                        3,
                        2,
                        null,
                        null,
                        new Guid("11111111-0000-0000-0000-000000000002")
                    },
                    {
                        new Guid("23333333-0000-0000-0000-000000000001"),
                        null,
                        new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                        null,
                        null,
                        null,
                        false,
                        2,
                        1,
                        null,
                        null,
                        new Guid("11111111-0000-0000-0000-000000000003")
                    },
                    {
                        new Guid("23333333-0000-0000-0000-000000000002"),
                        null,
                        new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                        null,
                        null,
                        null,
                        false,
                        3,
                        2,
                        null,
                        null,
                        new Guid("11111111-0000-0000-0000-000000000003")
                    }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "WorkflowSteps",
                keyColumn: "Id",
                keyValue: new Guid("21111111-0000-0000-0000-000000000001"));

            migrationBuilder.DeleteData(
                table: "WorkflowSteps",
                keyColumn: "Id",
                keyValue: new Guid("21111111-0000-0000-0000-000000000002"));

            migrationBuilder.DeleteData(
                table: "WorkflowSteps",
                keyColumn: "Id",
                keyValue: new Guid("22222222-0000-0000-0000-000000000001"));

            migrationBuilder.DeleteData(
                table: "WorkflowSteps",
                keyColumn: "Id",
                keyValue: new Guid("22222222-0000-0000-0000-000000000002"));

            migrationBuilder.DeleteData(
                table: "WorkflowSteps",
                keyColumn: "Id",
                keyValue: new Guid("23333333-0000-0000-0000-000000000001"));

            migrationBuilder.DeleteData(
                table: "WorkflowSteps",
                keyColumn: "Id",
                keyValue: new Guid("23333333-0000-0000-0000-000000000002"));

            migrationBuilder.DeleteData(
                table: "WorkflowDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("11111111-0000-0000-0000-000000000001"));

            migrationBuilder.DeleteData(
                table: "WorkflowDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("11111111-0000-0000-0000-000000000002"));

            migrationBuilder.DeleteData(
                table: "WorkflowDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("11111111-0000-0000-0000-000000000003"));
        }
    }
}