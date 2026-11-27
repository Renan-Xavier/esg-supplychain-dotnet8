using EsgSupplyChain.Api.Services;
using EsgSupplyChain.Api.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EsgSupplyChain.Api.Controllers;

[ApiController]
[Route("api/alertas")]
public class AlertasConformidadeController : ControllerBase
{
    private readonly IAlertaConformidadeService _alertaService;

    public AlertasConformidadeController(IAlertaConformidadeService alertaService)
    {
        _alertaService = alertaService;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<ResultadoPaginadoViewModel<AlertaConformidadeViewModel>>> Listar(
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanhoPagina = 10,
        [FromQuery] string? severidade = null,
        [FromQuery] bool? resolvido = null)
    {
        var resultado = await _alertaService.ListarPaginadoAsync(pagina, tamanhoPagina, severidade, resolvido);
        return Ok(resultado);
    }

    [HttpGet("painel-cadeia")]
    [AllowAnonymous]
    public async Task<ActionResult<PainelCadeiaSuprimentosViewModel>> ConsultarPainelCadeia()
    {
        var painel = await _alertaService.ObterPainelCadeiaAsync();
        return Ok(painel);
    }

    [HttpPatch("{id:long}/resolver")]
    [Authorize(Roles = "Auditor")]
    public async Task<ActionResult<AlertaConformidadeViewModel>> Resolver(long id, [FromBody] ResolverAlertaViewModel dados)
    {
        var resolvido = await _alertaService.ResolverAlertaAsync(id, dados);
        return Ok(resolvido);
    }
}
