using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EsgSupplyChain.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class CriacaoInicialEsg : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tb_fornecedores",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NomeEmpresa = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Cnpj = table.Column<string>(type: "nvarchar(14)", maxLength: 14, nullable: false),
                    Pais = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    NivelCadeia = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    MetaAnualCarbono = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    CertificadoDireitosHumanos = table.Column<bool>(type: "bit", nullable: false),
                    DataCadastro = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tb_fornecedores", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "tb_alertas_conformidade",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FornecedorId = table.Column<long>(type: "bigint", nullable: false),
                    TipoAlerta = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Severidade = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Mensagem = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    Resolvido = table.Column<bool>(type: "bit", nullable: false),
                    PlanoMitigacao = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    DataGeracao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DataResolucao = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tb_alertas_conformidade", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tb_alertas_conformidade_tb_fornecedores_FornecedorId",
                        column: x => x.FornecedorId,
                        principalTable: "tb_fornecedores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tb_emissoes_escopo3",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FornecedorId = table.Column<long>(type: "bigint", nullable: false),
                    CategoriaEscopo3 = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    QuantidadeCarbono = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    TipoDado = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    StatusAuditoria = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ObservacaoAuditoria = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    DataRelatorio = table.Column<DateOnly>(type: "date", nullable: false),
                    Ativo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tb_emissoes_escopo3", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tb_emissoes_escopo3_tb_fornecedores_FornecedorId",
                        column: x => x.FornecedorId,
                        principalTable: "tb_fornecedores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_tb_alertas_conformidade_FornecedorId_TipoAlerta_Resolvido",
                table: "tb_alertas_conformidade",
                columns: new[] { "FornecedorId", "TipoAlerta", "Resolvido" });

            migrationBuilder.CreateIndex(
                name: "IX_tb_alertas_conformidade_Resolvido_Severidade",
                table: "tb_alertas_conformidade",
                columns: new[] { "Resolvido", "Severidade" });

            migrationBuilder.CreateIndex(
                name: "IX_tb_emissoes_escopo3_FornecedorId_Ativo_StatusAuditoria",
                table: "tb_emissoes_escopo3",
                columns: new[] { "FornecedorId", "Ativo", "StatusAuditoria" });

            migrationBuilder.CreateIndex(
                name: "IX_tb_fornecedores_Cnpj",
                table: "tb_fornecedores",
                column: "Cnpj",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tb_fornecedores_NivelCadeia_CertificadoDireitosHumanos",
                table: "tb_fornecedores",
                columns: new[] { "NivelCadeia", "CertificadoDireitosHumanos" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tb_alertas_conformidade");

            migrationBuilder.DropTable(
                name: "tb_emissoes_escopo3");

            migrationBuilder.DropTable(
                name: "tb_fornecedores");
        }
    }
}
