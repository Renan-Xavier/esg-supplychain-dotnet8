using System.ComponentModel.DataAnnotations;

namespace EsgSupplyChain.Api.ViewModels;

public class TokenRequisicaoViewModel
{
    [Required(ErrorMessage = "O usuário operacional é obrigatório.")]
    public string Usuario { get; set; } = string.Empty;

    [Required(ErrorMessage = "A senha é obrigatória.")]
    public string Senha { get; set; } = string.Empty;
}

public class TokenRespostaViewModel
{
    public string AccessToken { get; init; } = string.Empty;
    public string Perfil { get; init; } = string.Empty;
    public DateTime ExpiraEmUtc { get; init; }
}
