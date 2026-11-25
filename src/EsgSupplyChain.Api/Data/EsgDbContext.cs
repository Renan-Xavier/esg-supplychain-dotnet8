using EsgSupplyChain.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace EsgSupplyChain.Api.Data;

public class EsgDbContext : DbContext
{
    public EsgDbContext(DbContextOptions<EsgDbContext> options) : base(options)
    {
    }

    public DbSet<Fornecedor> Fornecedores => Set<Fornecedor>();
    public DbSet<EmissaoEscopo3> Emissoes => Set<EmissaoEscopo3>();
    public DbSet<AlertaConformidade> AlertasConformidade => Set<AlertaConformidade>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Fornecedor>(entity =>
        {
            entity.ToTable("tb_fornecedores");
            entity.HasKey(f => f.Id);

            entity.Property(f => f.NomeEmpresa).HasMaxLength(150).IsRequired();
            entity.Property(f => f.Cnpj).HasMaxLength(14).IsRequired();
            entity.Property(f => f.Pais).HasMaxLength(80).IsRequired();
            entity.Property(f => f.NivelCadeia).HasMaxLength(20).IsRequired();
            entity.Property(f => f.MetaAnualCarbono).HasPrecision(12, 2).IsRequired();
            entity.Property(f => f.CertificadoDireitosHumanos).IsRequired();
            entity.Property(f => f.DataCadastro).IsRequired();

            entity.HasIndex(f => f.Cnpj).IsUnique();
            entity.HasIndex(f => new { f.NivelCadeia, f.CertificadoDireitosHumanos });
        });

        modelBuilder.Entity<EmissaoEscopo3>(entity =>
        {
            entity.ToTable("tb_emissoes_escopo3");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.CategoriaEscopo3).HasMaxLength(100).IsRequired();
            entity.Property(e => e.QuantidadeCarbono).HasPrecision(12, 2).IsRequired();
            entity.Property(e => e.TipoDado).HasMaxLength(30).IsRequired();
            entity.Property(e => e.StatusAuditoria).HasMaxLength(30).IsRequired();
            entity.Property(e => e.ObservacaoAuditoria).HasMaxLength(300);
            entity.Property(e => e.DataRelatorio).IsRequired();
            entity.Property(e => e.Ativo).IsRequired();

            entity.HasIndex(e => new { e.FornecedorId, e.Ativo, e.StatusAuditoria });

            entity.HasOne(e => e.Fornecedor)
                .WithMany(f => f.Emissoes)
                .HasForeignKey(e => e.FornecedorId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AlertaConformidade>(entity =>
        {
            entity.ToTable("tb_alertas_conformidade");
            entity.HasKey(a => a.Id);

            entity.Property(a => a.TipoAlerta).HasMaxLength(50).IsRequired();
            entity.Property(a => a.Severidade).HasMaxLength(20).IsRequired();
            entity.Property(a => a.Mensagem).HasMaxLength(400).IsRequired();
            entity.Property(a => a.PlanoMitigacao).HasMaxLength(400);
            entity.Property(a => a.Resolvido).IsRequired();
            entity.Property(a => a.DataGeracao).IsRequired();

            entity.HasIndex(a => new { a.Resolvido, a.Severidade });
            entity.HasIndex(a => new { a.FornecedorId, a.TipoAlerta, a.Resolvido });

            entity.HasOne(a => a.Fornecedor)
                .WithMany(f => f.Alertas)
                .HasForeignKey(a => a.FornecedorId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
