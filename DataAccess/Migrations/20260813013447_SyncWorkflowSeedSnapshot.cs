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
            migrationBuilder.Sql("""
    IF EXISTS (
        SELECT 1
        FROM sys.identity_columns
        WHERE object_id = OBJECT_ID(N'[dbo].[Departments]')
          AND name = 'Id'
    )
        SET IDENTITY_INSERT [dbo].[Departments] ON;

    IF NOT EXISTS (SELECT 1 FROM [dbo].[Departments] WHERE [Id] = 1)
        INSERT INTO [dbo].[Departments]
            ([Id], [Code], [Name], [ParentDepartmentId], [CreatedAt], [IsDeleted])
        VALUES
            (1, N'ROADS', N'Roads & Infrastructure', NULL, '2026-01-01T00:00:00', 0);

    IF NOT EXISTS (SELECT 1 FROM [dbo].[Departments] WHERE [Id] = 2)
        INSERT INTO [dbo].[Departments]
            ([Id], [Code], [Name], [ParentDepartmentId], [CreatedAt], [IsDeleted])
        VALUES
            (2, N'SANITATION', N'Sanitation', NULL, '2026-01-01T00:00:00', 0);

    IF NOT EXISTS (SELECT 1 FROM [dbo].[Departments] WHERE [Id] = 3)
        INSERT INTO [dbo].[Departments]
            ([Id], [Code], [Name], [ParentDepartmentId], [CreatedAt], [IsDeleted])
        VALUES
            (3, N'LIGHTING', N'Street Lighting', NULL, '2026-01-01T00:00:00', 0);

    IF EXISTS (
        SELECT 1
        FROM sys.identity_columns
        WHERE object_id = OBJECT_ID(N'[dbo].[Departments]')
          AND name = 'Id'
    )
        SET IDENTITY_INSERT [dbo].[Departments] OFF;


    IF EXISTS (
        SELECT 1
        FROM sys.identity_columns
        WHERE object_id = OBJECT_ID(N'[dbo].[Roles]')
          AND name = 'Id'
    )
        SET IDENTITY_INSERT [dbo].[Roles] ON;

    IF NOT EXISTS (SELECT 1 FROM [dbo].[Roles] WHERE [Id] = 1)
        INSERT INTO [dbo].[Roles]
            ([Id], [Code], [IsDepartmentScoped], [CreatedAt], [IsDeleted])
        VALUES
            (1, N'Admin', 0, '2026-01-01T00:00:00', 0);

    IF NOT EXISTS (SELECT 1 FROM [dbo].[Roles] WHERE [Id] = 2)
        INSERT INTO [dbo].[Roles]
            ([Id], [Code], [IsDepartmentScoped], [CreatedAt], [IsDeleted])
        VALUES
            (2, N'Supervisor', 1, '2026-01-01T00:00:00', 0);

    IF NOT EXISTS (SELECT 1 FROM [dbo].[Roles] WHERE [Id] = 3)
        INSERT INTO [dbo].[Roles]
            ([Id], [Code], [IsDepartmentScoped], [CreatedAt], [IsDeleted])
        VALUES
            (3, N'Agent', 1, '2026-01-01T00:00:00', 0);

    IF NOT EXISTS (SELECT 1 FROM [dbo].[Roles] WHERE [Id] = 4)
        INSERT INTO [dbo].[Roles]
            ([Id], [Code], [IsDepartmentScoped], [CreatedAt], [IsDeleted])
        VALUES
            (4, N'Citizen', 0, '2026-01-01T00:00:00', 0);

    IF EXISTS (
        SELECT 1
        FROM sys.identity_columns
        WHERE object_id = OBJECT_ID(N'[dbo].[Roles]')
          AND name = 'Id'
    )
        SET IDENTITY_INSERT [dbo].[Roles] OFF;
    """);
            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "Id", "Code", "IsDepartmentScoped", "CreatedAt", "IsDeleted" },
                values: new object[,]
                {
                    { 1, "Admin", false, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), false },
                    { 2, "Supervisor", true, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), false },
                    { 3, "Agent", true, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), false },
                    { 4, "Citizen", false, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), false }
                });

            migrationBuilder.InsertData(
                table: "Departments",
                columns: new[] { "Id", "Code", "Name", "ParentDepartmentId", "CreatedAt", "IsDeleted" },
                values: new object[,]
                {
                    { 1, "ROADS", "Roads & Infrastructure", null, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), false },
                    { 2, "SANITATION", "Sanitation", null, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), false },
                    { 3, "LIGHTING", "Street Lighting", null, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), false }
                });

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