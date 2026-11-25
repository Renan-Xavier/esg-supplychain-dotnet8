# Tarefas

| ID | Tarefa | Origem | Depende de | Concluída quando |
|---|---|---|---|---|
| T-01 | Estruturar solução `.sln`, projeto Web API (.NET 8) e projeto de testes xUnit | Constituição, D-01 | - | Solução compila limpa com `dotnet build` |
| T-02 | Modelar entidades `Fornecedor`, `EmissaoEscopo3` e `AlertaConformidade` | RF-01, RF-02, RF-05, Modelo de dados | T-01 | Classes de domínio representam regras e relacionamentos 1:N |
| T-03 | Configurar `EsgDbContext`, índices de performance, carga inicial e Migration EF Core | Constituição, D-02, D-03 | T-02 | Arquivos de migração gerados em `Data/Migrations` com índices e relacionamentos |
| T-04 | Implementar exceções de domínio e `TratadorGlobalExcecoes` (`ProblemDetails`) | Constituição, Casos de borda, D-06 | T-01 | Falhas 400, 404 e 422 retornam JSON RFC 7807 em português |
| T-05 | Criar `ViewModels` de requisição, resposta e `ResultadoPaginadoViewModel<T>` | Constituição, D-01, D-03 | T-02 | Nenhum endpoint expõe entidade de banco diretamente e paginação possui contrato único |
| T-06 | Implementar `FornecedorService` e `EmissaoService` com cálculo ESG e alertas reativos | RF-01 a RF-05, RN-01 a RN-06, D-05 | T-03, T-04, T-05 | Regras de CNPJ único, teto de 80%/100% de carbono e inativação lógica funcionam isoladas |
| T-07 | Implementar `AlertaConformidadeService` com painel executivo da cadeia e mitigação | RF-05, RF-06, RN-06, RN-07 | T-06 | Alertas listam paginados, consolidam indicadores da cadeia e exigem plano para resolução |
| T-08 | Configurar autenticação JWT Bearer, perfis (`Analista`, `Auditor`) e `AuthController` | RF-07, D-04 | T-01, T-05 | Token válido libera rotas protegidas e bloqueia perfis sem permissão (`401`/`403`) |
| T-09 | Implementar `FornecedoresController`, `EmissoesController` e `AlertasConformidadeController` | RF-01 a RF-06, Contratos | T-06, T-07, T-08 | Todos os endpoints REST operam via serviços com paginação e autorização aplicadas |
| T-10 | Escrever testes unitários de regras ESG e testes de integração HTTP 200 por controller | Qualidade (Constituição), Critérios de aceite, D-07 | T-09 | `dotnet test` aprova testes unitários e pelo menos 1 teste HTTP 200 para cada controller |
| T-11 | Criar `Dockerfile` multi-stage, `.dockerignore` e coleção Postman completa | Execução local, Entrega FIAP | T-09 | `Dockerfile` e coleção Postman cobrem autenticação e todos os fluxos de negócio |
| T-12 | Documentar `README.md` final e validar pacote completo de entrega | Critérios de aceite | T-10, T-11 | Build, testes, migrations e documentação conferidos de ponta a ponta |
