using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using EsgSupplyChain.Api.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace EsgSupplyChain.Tests;

public class ControllersIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ControllersIntegrationTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Fornecedores_Get_ReturnsHttpStatusCode200()
    {
        // Arrange
        var request = "/api/fornecedores?pagina=1&tamanhoPagina=10";

        // Act
        var response = await _client.GetAsync(request);

        // Assert
        response.EnsureSuccessStatusCode();
        var pagina = await response.Content.ReadFromJsonAsync<ResultadoPaginadoViewModel<FornecedorRespostaViewModel>>();
        Assert.NotNull(pagina);
        Assert.True(pagina.TotalItens >= 3);
    }

    [Fact]
    public async Task Emissoes_Get_ReturnsHttpStatusCode200()
    {
        // Arrange
        var request = "/api/emissoes?pagina=1&tamanhoPagina=10";

        // Act
        var response = await _client.GetAsync(request);

        // Assert
        response.EnsureSuccessStatusCode();
        var pagina = await response.Content.ReadFromJsonAsync<ResultadoPaginadoViewModel<EmissaoRespostaViewModel>>();
        Assert.NotNull(pagina);
        Assert.NotEmpty(pagina.Itens);
    }

    [Fact]
    public async Task AlertasConformidade_Get_ReturnsHttpStatusCode200()
    {
        // Arrange
        var request = "/api/alertas?pagina=1&tamanhoPagina=10";

        // Act
        var response = await _client.GetAsync(request);

        // Assert
        response.EnsureSuccessStatusCode();
        var pagina = await response.Content.ReadFromJsonAsync<ResultadoPaginadoViewModel<AlertaConformidadeViewModel>>();
        Assert.NotNull(pagina);
        Assert.NotEmpty(pagina.Itens);
    }

    [Fact]
    public async Task Auth_GetPerfis_ReturnsHttpStatusCode200()
    {
        // Arrange
        var request = "/api/auth/perfis";

        // Act
        var response = await _client.GetAsync(request);

        // Assert
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task PostFornecedor_SemTokenJwt_Retorna401Unauthorized()
    {
        var novo = new FornecedorRequisicaoViewModel
        {
            NomeEmpresa = "Logística Sem Token Ltda.",
            Cnpj = "33444555000188",
            Pais = "Brasil",
            NivelCadeia = "TIER_1",
            MetaAnualCarbono = 200m,
            CertificadoDireitosHumanos = true
        };

        var response = await _client.PostAsJsonAsync("/api/fornecedores", novo);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task FluxoAutenticado_AnalistaCadastra_MasNaoPodeAuditar_AuditorAprova()
    {
        var tokenAnalista = await ObterTokenAsync("analista", "esg-analista-2026");
        var tokenAuditor = await ObterTokenAsync("auditor", "esg-auditor-2026");

        using var reqCadastro = new HttpRequestMessage(HttpMethod.Post, "/api/fornecedores")
        {
            Content = JsonContent.Create(new FornecedorRequisicaoViewModel
            {
                NomeEmpresa = "BioPlásticos Atlântico S.A.",
                Cnpj = "77888999000122",
                Pais = "Brasil",
                NivelCadeia = "TIER_2",
                MetaAnualCarbono = 100m,
                CertificadoDireitosHumanos = true
            })
        };
        reqCadastro.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenAnalista);

        var respCadastro = await _client.SendAsync(reqCadastro);
        Assert.Equal(HttpStatusCode.Created, respCadastro.StatusCode);
        var fornecedor = await respCadastro.Content.ReadFromJsonAsync<FornecedorRespostaViewModel>();
        Assert.NotNull(fornecedor);

        using var reqEmissao = new HttpRequestMessage(HttpMethod.Post, "/api/emissoes")
        {
            Content = JsonContent.Create(new EmissaoRequisicaoViewModel
            {
                FornecedorId = fornecedor.Id,
                CategoriaEscopo3 = "Resíduos gerados nas operações",
                QuantidadeCarbono = 85m,
                TipoDado = "PRIMARIO_MEDIDO",
                DataRelatorio = new DateOnly(2026, 11, 20)
            })
        };
        reqEmissao.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenAnalista);

        var respEmissao = await _client.SendAsync(reqEmissao);
        Assert.Equal(HttpStatusCode.Created, respEmissao.StatusCode);
        var emissao = await respEmissao.Content.ReadFromJsonAsync<EmissaoRespostaViewModel>();
        Assert.NotNull(emissao);

        // Analista tentando aprovar auditoria deve receber 403 Forbidden
        using var reqAuditoriaAnalista = new HttpRequestMessage(HttpMethod.Patch, $"/api/emissoes/{emissao.Id}/auditoria")
        {
            Content = JsonContent.Create(new EmissaoAuditoriaViewModel
            {
                StatusAuditoria = "AUDITADO",
                ObservacaoAuditoria = "Tentativa sem perfil de auditor"
            })
        };
        reqAuditoriaAnalista.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenAnalista);
        var respProibida = await _client.SendAsync(reqAuditoriaAnalista);
        Assert.Equal(HttpStatusCode.Forbidden, respProibida.StatusCode);

        // Auditor aprova com sucesso
        using var reqAuditoriaAuditor = new HttpRequestMessage(HttpMethod.Patch, $"/api/emissoes/{emissao.Id}/auditoria")
        {
            Content = JsonContent.Create(new EmissaoAuditoriaViewModel
            {
                StatusAuditoria = "AUDITADO",
                ObservacaoAuditoria = "Inventário GEE conferido."
            })
        };
        reqAuditoriaAuditor.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenAuditor);
        var respAprovada = await _client.SendAsync(reqAuditoriaAuditor);
        respAprovada.EnsureSuccessStatusCode();

        // Verifica que o consumo de 85% gerou status ALERTA_LIMITE_CARBONO
        var respConformidade = await _client.GetAsync($"/api/fornecedores/{fornecedor.Id}/conformidade");
        respConformidade.EnsureSuccessStatusCode();
        var diagnostico = await respConformidade.Content.ReadFromJsonAsync<ConformidadeFornecedorViewModel>();
        Assert.NotNull(diagnostico);
        Assert.Equal("ALERTA_LIMITE_CARBONO", diagnostico.StatusConformidade);
        Assert.Equal(1, diagnostico.AlertasAbertos);
    }

    [Fact]
    public async Task PaginacaoInvalida_Retorna400ProblemDetailsEmPortugues()
    {
        var response = await _client.GetAsync("/api/fornecedores?pagina=0&tamanhoPagina=100");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Contains("paginação", problem.Detail, StringComparison.OrdinalIgnoreCase);
    }

    private async Task<string> ObterTokenAsync(string usuario, string senha)
    {
        var response = await _client.PostAsJsonAsync("/api/auth/token", new TokenRequisicaoViewModel
        {
            Usuario = usuario,
            Senha = senha
        });

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<TokenRespostaViewModel>();
        return payload!.AccessToken;
    }
}
