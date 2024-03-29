using Azure.Identity;
using Microsoft.EntityFrameworkCore.Migrations;
using Package.Infrastructure.Data;

#nullable disable

namespace Infrastructure.Data.Migrations;

/// <inheritdoc />
public partial class InitialCreate : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(
            name: "todo");

        migrationBuilder.CreateTable(
            name: "SystemSetting",
            schema: "todo",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Key = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                Value = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                Flags = table.Column<int>(type: "int", nullable: false),
                RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                CreatedDate = table.Column<DateTime>(type: "datetime2(0)", nullable: false),
                CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                UpdatedDate = table.Column<DateTime>(type: "datetime2(0)", nullable: true),
                UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SystemSetting", x => x.Id)
                    .Annotation("SqlServer:Clustered", false);
            });

        migrationBuilder.CreateTable(
            name: "TodoItem",
            schema: "todo",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                Status = table.Column<int>(type: "int", nullable: false),
                SecureRandom = table.Column<byte[]>(type: "varbinary(100)", maxLength: 100, nullable: true),
                SecureDeterministic = table.Column<byte[]>(type: "varbinary(100)", maxLength: 100, nullable: true),
                IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                CreatedDate = table.Column<DateTime>(type: "datetime2(0)", nullable: false),
                CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                UpdatedDate = table.Column<DateTime>(type: "datetime2(0)", nullable: true),
                UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_TodoItem", x => x.Id)
                    .Annotation("SqlServer:Clustered", false);
            });

        migrationBuilder.CreateIndex(
            name: "IX_SystemSetting_Key",
            schema: "todo",
            table: "SystemSetting",
            column: "Key",
            unique: true)
            .Annotation("SqlServer:Clustered", true);

        migrationBuilder.CreateIndex(
            name: "IX_TodoItem_Name",
            schema: "todo",
            table: "TodoItem",
            column: "Name",
            unique: true)
            .Annotation("SqlServer:Clustered", true);

        //add to migration class - customize for always encrypted (until supported in fluent syntax)
        string url_AKV_CMK = "[AKV-Url-CMK]";
        string schema_table = "[todo].[TodoItem]";
        string cmkName = "CMK_WITH_AKV";

        var support = new MigrationSupport(migrationBuilder, new DefaultAzureCredential());
        support.CreateColumnMasterKey(url_AKV_CMK, cmkName);

        string cekName = "CEK_WITH_AKV";
        support.CreateColumnEncryptionKey(url_AKV_CMK, cmkName, cekName);

        string colDef = "[SecureDeterministic] varbinary(100)"; //non string so collate: null
        support.AlterColumnEncryption(cekName, schema_table, colDef, collate: null, encType: "DETERMINISTIC");

        colDef = "[SecureRandom] varbinary(100)"; //non string so collate: null
        support.AlterColumnEncryption(cekName, schema_table, colDef, collate: null, encType: "RANDOMIZED");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "SystemSetting",
            schema: "todo");

        migrationBuilder.DropTable(
            name: "TodoItem",
            schema: "todo");
    }
}
