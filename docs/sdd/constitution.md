# Constituição do Projeto

## Stack
* C# 12 com ASP.NET Core 8 para construção do Webservice RESTful
* Persistência relacional via Entity Framework Core 8 com Migrations versionadas (SQL Server LocalDB / Oracle FIAP)
* Testes automatizados com xUnit e `Microsoft.AspNetCore.Mvc.Testing`
* Containerização via Docker (build multi-stage)

## Arquitetura
* Padrão MVVM aplicado à Web API: `Models` representam o domínio e persistência, `ViewModels` isolam contratos de entrada/saída da apresentação, `Services` concentram regras de negócio e `Controllers` apenas orquestram requisições HTTP
* Nenhum Controller acessa `DbContext` ou entidades de persistência diretamente; toda operação passa pela camada de serviço
* Webservice desacoplado de interface visual (sem servir HTML estático), expondo contratos JSON documentados via OpenAPI/Swagger e política explícita de CORS
* Nenhuma biblioteca externa entra na solução sem justificativa registrada no plano técnico

## Qualidade
* Toda regra crítica de cálculo de emissões, conformidade ESG e geração de alertas possui teste unitário
* Todo Controller da API possui pelo menos um teste automatizado em xUnit validando retorno HTTP 200 no fluxo principal
* Todos os endpoints de listagem aplicam paginação obrigatória e consultas otimizadas (`AsNoTracking`)
* Erros de validação, regra de negócio e recursos ausentes retornam `ProblemDetails` (RFC 7807) em português

## Convenções
* Termos de domínio ESG e mensagens de retorno/erro em português claro; sufixos arquiteturais seguem o padrão do ecossistema .NET (`Controller`, `Service`, `ViewModel`, `DbContext`)
* Commits no formato semântico: `tipo(escopo): descrição objetiva do que foi alterado`

## Governança
* Se o plano técnico contradiz a constituição, quem muda é o plano
* Se a especificação contradiz a constituição, quem muda é a especificação
