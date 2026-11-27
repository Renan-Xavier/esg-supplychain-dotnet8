using EsgSupplyChain.Api.Services;
using EsgSupplyChain.Api.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EsgSupplyChain.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly ITokenService _tokenService;

    public AuthController(ITokenService tokenService)
    {
        _tokenService = tokenService;
    }

    [HttpPost("token")]
    [AllowAnonymous]
    public ActionResult<TokenRespostaViewModel> EmitirToken([FromBody] TokenRequisicaoViewModel requisicao)
    {
        var resultado = _tokenService.AutenticarGerarToken(requisicao);
        if (resultado is null)
        {
            return Unauthorized(new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Credenciais inválidas",
                Detail = "Usuário operacional ou senha incorretos."
            });
        }

        return Ok(resultado);
    }

    [HttpGet("perfis")]
    [AllowAnonymous]
    public ActionResult<object> ConsultarPerfisSuportados()
    {
        return Ok(new
        {
            Perfis = new[]
            {
                new { Perfil = "Analista", Permissoes = "Cadastro de fornecedores e registro de relatórios de Escopo 3." },
                new { Perfil = "Auditor", Permissoes = "Aprovação/rejeição de auditorias, inativação de emissões e resolução de alertas ESG." }
            }
        });
    }
}
