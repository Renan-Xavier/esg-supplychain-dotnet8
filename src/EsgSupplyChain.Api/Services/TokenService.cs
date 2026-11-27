using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using EsgSupplyChain.Api.ViewModels;
using Microsoft.IdentityModel.Tokens;

namespace EsgSupplyChain.Api.Services;

public interface ITokenService
{
    TokenRespostaViewModel? AutenticarGerarToken(TokenRequisicaoViewModel credenciais);
}

public class TokenService : ITokenService
{
    private readonly IConfiguration _configuration;

    public TokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public TokenRespostaViewModel? AutenticarGerarToken(TokenRequisicaoViewModel credenciais)
    {
        var listaCredenciais = _configuration
            .GetSection("CredenciaisOperacionais")
            .Get<List<CredencialConfigurada>>() ?? new List<CredencialConfigurada>();

        var conta = listaCredenciais.FirstOrDefault(c =>
            string.Equals(c.Usuario, credenciais.Usuario?.Trim(), StringComparison.OrdinalIgnoreCase)
            && c.Senha == credenciais.Senha);

        if (conta is null)
        {
            return null;
        }

        var chaveSecreta = _configuration["Jwt:ChaveSecreta"]
            ?? throw new InvalidOperationException("Configuração Jwt:ChaveSecreta não definida.");
        var issuer = _configuration["Jwt:Issuer"] ?? "EsgSupplyChain.Api";
        var audience = _configuration["Jwt:Audience"] ?? "EsgSupplyChain.Clientes";
        var expiracaoMinutos = _configuration.GetValue<int?>("Jwt:ExpiracaoMinutos") ?? 120;

        var expiraEm = DateTime.UtcNow.AddMinutes(expiracaoMinutos);
        var chave = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(chaveSecreta));
        var assinatura = new SigningCredentials(chave, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, conta.Usuario),
            new Claim(ClaimTypes.Name, conta.Usuario),
            new Claim(ClaimTypes.Role, conta.Perfil),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var jwt = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: expiraEm,
            signingCredentials: assinatura);

        return new TokenRespostaViewModel
        {
            AccessToken = new JwtSecurityTokenHandler().WriteToken(jwt),
            Perfil = conta.Perfil,
            ExpiraEmUtc = expiraEm
        };
    }

    private sealed class CredencialConfigurada
    {
        public string Usuario { get; set; } = string.Empty;
        public string Senha { get; set; } = string.Empty;
        public string Perfil { get; set; } = string.Empty;
    }
}
