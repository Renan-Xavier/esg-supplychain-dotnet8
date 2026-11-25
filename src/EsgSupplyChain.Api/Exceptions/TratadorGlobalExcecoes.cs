using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace EsgSupplyChain.Api.Exceptions;

public class TratadorGlobalExcecoes : IExceptionHandler
{
    private readonly ILogger<TratadorGlobalExcecoes> _logger;

    public TratadorGlobalExcecoes(ILogger<TratadorGlobalExcecoes> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (status, titulo) = exception switch
        {
            RecursoNaoEncontradoException => (StatusCodes.Status404NotFound, "Recurso não encontrado"),
            RegraDeNegocioException => (StatusCodes.Status422UnprocessableEntity, "Violação de regra de negócio ESG"),
            ArgumentException => (StatusCodes.Status400BadRequest, "Parâmetro inválido na requisição"),
            _ => (StatusCodes.Status500InternalServerError, "Erro interno inesperado no servidor")
        };

        if (status == StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(exception, "Falha não tratada ao processar {Path}", httpContext.Request.Path);
        }

        var problemDetails = new ProblemDetails
        {
            Status = status,
            Title = titulo,
            Detail = status == StatusCodes.Status500InternalServerError
                ? "Ocorreu um erro interno ao processar a solicitação."
                : exception.Message,
            Instance = httpContext.Request.Path
        };

        httpContext.Response.StatusCode = status;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
        return true;
    }
}
