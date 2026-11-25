# Plano técnico

## Contexto

Materializa a especificação da **ESG Supply Chain API** para o desafio *Webservice com ASP.NET Core 8* (Fase 4 — Frameworks .NET da FIAP), evoluindo o domínio de Transparência na Cadeia de Suprimentos (Tema ESG 6) construído anteriormente em Java/Spring Boot para uma arquitetura corporativa em .NET 8 com MVVM, EF Core Migrations, paginação otimizada, segurança JWT, testes xUnit e Docker.

*(Adaptação SDD aplicada em relação ao template genérico: foram removidas premissas de aplicação monolítica com HTML na mesma origem e calendário visual, substituídas por contratos REST paginados, CORS explícito para clientes externos, autenticação stateless JWT sem CRUD de usuários e versionamento de banco via EF Core Migrations.)*

## Arquitetura

- **Apresentação (MVVM — `Controllers` + `ViewModels`):** Controllers REST (`FornecedoresController`, `EmissoesController`, `AlertasConformidadeController`, `AuthController`) recebem e devolvem exclusivamente `ViewModels` tipados (`*RequisicaoViewModel`, `*RespostaViewModel`, `ResultadoPaginadoViewModel<T>`).
- **Negócio (`Services`):** `FornecedorService`, `EmissaoService`, `AlertaConformidadeService` e `TokenService` concentram validações de domínio, cálculo de teto de carbono, geração/atualização de alertas e mapeamento Model ↔ ViewModel.
- **Persistência (`Data` + `Models`):** `EsgDbContext` (Entity Framework Core 8) mapeia `Fornecedor`, `EmissaoEscopo3` e `AlertaConformidade` com índices explícitos para filtros e chaves estrangeiras, gerenciado por Migrations.
- **Transversal (`Exceptions` + Segurança):** `TratadorGlobalExcecoes` (`IExceptionHandler` nativo do .NET 8) padroniza falhas em `ProblemDetails` (RFC 7807); middleware `JwtBearer` aplica autenticação e políticas de autorização por Role (`Auditor`, `Analista`).

## Decisões

- **D-01 (MVVM em Web API):** adotar separação estrita onde entidades do EF Core (`Models`) nunca vazam nos endpoints; toda entrada passa por validação em `ViewModels` e toda resposta, incluindo envelopes de paginação e diagnósticos ESG, é projetada em `ViewModels`.
- **D-02 (Banco de dados e Migrations):** configurar `Microsoft.EntityFrameworkCore.SqlServer` apontando por padrão para o SQL Server LocalDB do Visual Studio (`(localdb)\\mssqllocaldb`), mantendo suporte direto à troca de connection string para Oracle FIAP ou container SQL Server via variáveis de ambiente, com histórico versionado em `Data/Migrations`.
- **D-03 (Otimização de consultas e paginação):** utilizar `AsNoTracking()` em todas as leituras, projeções enxutas, ordenação determinística e índices compostos em `(FornecedorId, Ativo, StatusAuditoria)` e `(Resolvido, Severidade)` para garantir baixa latência sob alto volume.
- **D-04 (Segurança sem CRUD de usuários):** atender à exigência de proteger endpoints críticos sem desviar foco para manutenção de usuários; `POST /api/auth/token` valida perfis operacionais configurados (`auditor` e `analista`) e emite JWT assinado (HMAC-SHA256) com claims de Role.
- **D-05 (Motor reativo de alertas ESG):** acionar a verificação de conformidade de forma transacional ao cadastrar/atualizar fornecedor ou registrar/auditar emissão, mantendo no máximo um alerta aberto por categoria de risco para cada fornecedor (idempotência de alertas).
- **D-06 (Padronização de erros):** implementar `IExceptionHandler` + `AddProblemDetails()` do ASP.NET Core 8 traduzindo `RegraDeNegocioException` (`422`), `RecursoNaoEncontradoException` (`404`) e falhas de `ModelState` (`400`) para mensagens claras em português.
- **D-07 (Testabilidade em memória isolada):** substituir apenas o provider do `EsgDbContext` por banco isolado em memória na `CustomWebApplicationFactory` do projeto `EsgSupplyChain.Tests`, permitindo executar `dotnet test` em qualquer máquina ou pipeline CI sem depender de instância externa ativa.
- **D-08 (Justificativa de pacotes NuGet):**
  - `Microsoft.EntityFrameworkCore.SqlServer` / `Design`: persistência relacional e geração de Migrations.
  - `Microsoft.AspNetCore.Authentication.JwtBearer`: autenticação e autorização JWT padrão .NET 8.
  - `Swashbuckle.AspNetCore`: documentação interativa OpenAPI/Swagger.
  - `xUnit`, `Microsoft.AspNetCore.Mvc.Testing`, `Microsoft.EntityFrameworkCore.InMemory`: testes unitários e de integração dos controllers.

## Modelo de dados

**`Fornecedor` (`tb_fornecedores`)**
- `Id` (long, PK), `NomeEmpresa` (varchar 150), `Cnpj` (varchar 14, Unique Index), `Pais` (varchar 80), `NivelCadeia` (`TIER_1` | `TIER_2` | `TIER_3`), `MetaAnualCarbono` (decimal 12,2 — em `tCO2e`), `CertificadoDireitosHumanos` (bool), `DataCadastro` (DateTime UTC).

**`EmissaoEscopo3` (`tb_emissoes_escopo3`)**
- `Id` (long, PK), `FornecedorId` (long, FK -> `tb_fornecedores`), `CategoriaEscopo3` (varchar 100), `QuantidadeCarbono` (decimal 12,2), `TipoDado` (`PRIMARIO_MEDIDO` | `ESTIMADO_MEDIA`), `StatusAuditoria` (`PENDENTE` | `AUDITADO` | `REJEITADO` | `INVALIDADO`), `ObservacaoAuditoria` (varchar 300, opcional), `DataRelatorio` (DateOnly), `Ativo` (bool).

**`AlertaConformidade` (`tb_alertas_conformidade`)**
- `Id` (long, PK), `FornecedorId` (long, FK -> `tb_fornecedores`), `TipoAlerta` (`SEM_CERTIFICACAO_DH` | `LIMITE_PREVENTIVO_80` | `META_CARBONO_EXCEDIDA`), `Severidade` (`ALTA` | `CRITICA`), `Mensagem` (varchar 400), `Resolvido` (bool), `PlanoMitigacao` (varchar 400, opcional), `DataGeracao` (DateTime UTC), `DataResolucao` (DateTime UTC, opcional).

## Contratos

- **`POST /api/auth/token`** — recebe credencial operacional e devolve token JWT Bearer, perfil e expiração.
- **`GET /api/fornecedores?pagina=1&tamanhoPagina=10&nivelCadeia=&pais=&apenasSemCertificacao=`** — lista paginada de fornecedores (`HTTP 200`).
- **`GET /api/fornecedores/{id}`** — detalhes de um fornecedor (`HTTP 200` / `404`).
- **`POST /api/fornecedores`** *(Auth: Analista, Auditor)* — cadastra fornecedor e avalia risco trabalhista inicial (`HTTP 201` / `400` / `422`).
- **`PUT /api/fornecedores/{id}`** *(Auth: Analista, Auditor)* — atualiza dados cadastrais/meta anual e reavalia alertas (`HTTP 200` / `404` / `422`).
- **`GET /api/fornecedores/{id}/conformidade`** — calcula diagnóstico ESG completo do fornecedor, percentual consumido da meta anual e alertas ativos (`HTTP 200` / `404`).
- **`GET /api/emissoes?pagina=1&tamanhoPagina=10&fornecedorId=&statusAuditoria=`** — lista paginada de emissões ativas (`HTTP 200`).
- **`GET /api/emissoes/{id}`** — consulta relatório específico (`HTTP 200` / `404`).
- **`POST /api/emissoes`** *(Auth: Analista, Auditor)* — registra emissão de Escopo 3 e dispara verificação automática de limites 80%/100% (`HTTP 201` / `404` / `422`).
- **`PATCH /api/emissoes/{id}/auditoria`** *(Auth: Auditor)* — aprova ou rejeita relatório na auditoria ESG (`HTTP 200` / `403` / `404`).
- **`DELETE /api/emissoes/{id}`** *(Auth: Auditor)* — inativa logicamente relatório incorreto preservando trilha de auditoria (`HTTP 204` / `404`).
- **`GET /api/alertas?pagina=1&tamanhoPagina=10&severidade=&resolvido=`** — lista paginada de alertas socioambientais (`HTTP 200`).
- **`GET /api/alertas/painel-cadeia`** — indicadores consolidados da cadeia (total `tCO2e`, fornecedores por tier, conformes vs. críticos e alertas pendentes) (`HTTP 200`).
- **`PATCH /api/alertas/{id}/resolver`** *(Auth: Auditor)* — registra plano de mitigação e encerra o alerta (`HTTP 200` / `404` / `422`).

## Execução local

- **Com .NET 8 SDK + LocalDB:** executar `dotnet restore`, `dotnet ef database update --project src/EsgSupplyChain.Api` (ou deixar a inicialização automática em ambiente `Development` aplicar as migrations e carga inicial) e `dotnet run --project src/EsgSupplyChain.Api`. Swagger disponível em `http://localhost:5080/swagger`.
- **Testes automatizados:** `dotnet test` na raiz da solução executa toda a bateria xUnit (unitários + integração dos controllers).
- **Com Docker:** `docker build -t esg-supplychain-api .` na raiz do repositório.

## Riscos

- **Diferença de ambiente de banco na correção (LocalDB vs. Docker/Oracle):** mitigado mantendo Migrations padrão EF Core versionadas no projeto, string de conexão configurável via `ConnectionStrings__DefaultConnection` e suíte xUnit autossuficiente.
- **Crescimento da tabela de emissões degradando cálculo de conformidade:** mitigado com índice composto em `(FornecedorId, Ativo, StatusAuditoria)`, agregações SQL diretas no banco e paginação estrita limitada a no máximo 50 itens por página.
