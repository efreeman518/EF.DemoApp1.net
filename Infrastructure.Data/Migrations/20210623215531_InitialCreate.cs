using Microsoft.EntityFrameworkCore.Migrations;
using System;

namespace Infrastructure.Data.Migrations;

public partial class InitialCreate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(
            name: "todo");

        migrationBuilder.CreateTable(
            name: "TodoItem",
            schema: "todo",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                IsComplete = table.Column<bool>(type: "bit", nullable: false),
                Status = table.Column<int>(type: "int", nullable: false),
                CreatedDate = table.Column<DateTime>(type: "datetime2(0)", nullable: false),
                CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                UpdatedDate = table.Column<DateTime>(type: "datetime2(0)", nullable: false),
                UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_TodoItem", x => x.Id)
                    .Annotation("SqlServer:Clustered", false);
            });

        migrationBuilder.CreateIndex(
            name: "IX_TodoItem_Name",
            schema: "todo",
            table: "TodoItem",
            column: "Name",
            unique: true)
            .Annotation("SqlServer:Clustered", true);

        //column level encryption
        //customize for always encrypted (until supported in fluent syntax)
        //string url_AKV_CMK = "https://somevault.vault.azure.net/keys/SQL-ColMaskerKey-Default/abc123";
        //string url_AKV_CMK = Environment.GetEnvironmentVariable("AKVCMKURL")!;
        //string cmkName = "CMK_WITH_AKV";
        //string cekName = "CEK_WITH_AKV";
        //string schema_table = "[schema].[table]"; //this is the schema.table above 
        //string colDef = "[SecureString] nvarchar(100)"; //this is the column to secure defined in the table above 
        //var support = new MigrationSupport(migrationBuilder, new DefaultAzureCredential());
        //support.CreateColumnMasterKey(url_AKV_CMK, cmkName);
        //support.CreateColumnEncryptionKey(url_AKV_CMK, cmkName, cekName);
        //support.AlterColumnEncryption(cekName, schema_table, colDef);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "TodoItem",
            schema: "todo");
    }
}
