# ESG Supply Chain API (.NET 8) — Transparência e Conformidade na Cadeia de Suprimentos

## Problema

Empresas contratantes têm dificuldade para rastrear emissões indiretas de carbono (Escopo 3) e fiscalizar práticas de trabalho justo nos diferentes elos (Tier 1, Tier 2 e Tier 3) da cadeia de suprimentos. Sem limites contratuais monitorados continuamente e sem alertas automáticos de desvio, fornecedores ultrapassam tetos de emissão ou operam com certificações vencidas sem visibilidade para a governança ambiental.

## Objetivo

Construir um Webservice RESTful em ASP.NET Core 8 que evolui a plataforma ESG da fase anterior para monitorar fornecedores, auditar relatórios de emissão de Escopo 3, calcular índices de conformidade socioambiental em tempo real e disparar alertas automatizados com planos de mitigação quando metas contratuais ou requisitos trabalhistas são violados.

## Usuários

- **Analista de Sustentabilidade:** cadastra fornecedores, registra relatórios periódicos de emissões de Escopo 3 e acompanha indicadores da cadeia.
- **Auditor ESG:** valida ou reprova relatórios de emissões submetidos, audita conformidade por fornecedor e encerra alertas mediante plano de mitigação.
- **Sistemas Corporativos / Painéis Externos:** consomem listagens paginadas e o painel consolidado de governança da cadeia.

## Histórias

- Como **Analista de Sustentabilidade**, quero cadastrar fornecedores com meta anual máxima de carbono (`tCO2e`) e status de certificação trabalhista para monitorar riscos na cadeia de suprimentos.
- Como **Analista de Sustentabilidade**, quero registrar emissões de Escopo 3 vinculadas a um fornecedor e receber alertas automáticos caso o consumo acumulado atinja faixa crítica da meta anual.
- Como **Auditor ESG**, quero aprovar, rejeitar ou invalidar relatórios de emissão com rastreabilidade para garantir que apenas dados verificados componham o selo de conformidade.
- Como **Auditor ESG**, quero consultar o diagnóstico de conformidade individual de um fornecedor e o painel consolidado da cadeia para priorizar ações corretivas.

## Requisitos funcionais

- **RF-01:** cadastrar, consultar por ID, atualizar e listar fornecedores de forma paginada com filtros por país, nível da cadeia (`TIER_1`, `TIER_2`, `TIER_3`) e risco trabalhista.
- **RF-02:** registrar relatórios de emissões de carbono de Escopo 3 (`tCO2e`) e listá-los de forma paginada com filtros por fornecedor e status de auditoria.
- **RF-03:** permitir que auditores autenticados atualizem o parecer de auditoria de um relatório (`PENDENTE`, `AUDITADO`, `REJEITADO`) ou realizem sua inativação lógica preservando histórico.
- **RF-04:** calcular sob demanda o diagnóstico completo de conformidade ESG de um fornecedor comparando emissões ativas frente à meta anual contratual e certificação de direitos humanos.
- **RF-05:** gerar alertas automáticos de conformidade sempre que um fornecedor operar sem certificação trabalhista, atingir 80% do teto anual de carbono (alerta preventivo) ou ultrapassar 100% da meta (violação crítica).
- **RF-06:** listar alertas de conformidade com paginação, expor resumo executivo consolidado da cadeia e permitir resolução de alertas mediante registro de plano de mitigação.
- **RF-07:** autenticar chamadas a endpoints sensíveis via token JWT Bearer com controle de acesso por perfil (`Analista` e `Auditor`).

## Regras de negócio

- **RN-01:** não é permitido cadastrar dois fornecedores com o mesmo CNPJ (comparação normalizada apenas pelos 14 dígitos numéricos).
- **RN-02:** a meta anual de emissões (`MetaAnualCarbono`) de todo fornecedor e a quantidade reportada (`QuantidadeCarbono`) em qualquer relatório devem ser estritamente maiores que zero.
- **RN-03:** todo relatório de emissão nasce com status `PENDENTE` e flag `Ativo = true`; a exclusão via API nunca apaga o registro físico, marcando `Ativo = false` e status `INVALIDADO`.
- **RN-04:** relatórios inativados (`INVALIDADO`) ou `REJEITADO` não entram no cálculo do volume acumulado de carbono do fornecedor.
- **RN-05:** classificação automática de conformidade do fornecedor:
  - `BLOQUEADO_DIREITOS_HUMANOS`: quando `CertificadoDireitosHumanos` for falso (gera alerta `CRITICA`).
  - `META_CARBONO_EXCEDIDA`: quando a soma de emissões válidas ultrapassar 100% da `MetaAnualCarbono` (gera alerta `CRITICA`).
  - `ALERTA_LIMITE_CARBONO`: quando a soma atingir entre 80% e 100% da `MetaAnualCarbono` (gera alerta `ALTA`).
  - `PENDENTE_REPORTE_ESCOPO3`: quando não houver emissões ativas válidas registradas.
  - `EM_ANALISE_AUDITORIA`: quando houver emissões dentro da meta, mas nenhuma com status `AUDITADO`.
  - `CONFORME_AUDITADO`: certificação trabalhista válida, emissões dentro de 80% da meta e ao menos um relatório `AUDITADO`.
- **RN-06:** não devem ser duplicados alertas abertos (`Resolvido = false`) do mesmo tipo para o mesmo fornecedor; caso já exista alerta aberto equivalente, o sistema atualiza a mensagem/medição vigente.
- **RN-07:** para resolver um alerta de conformidade, é obrigatório informar uma descrição de plano de ação/mitigação com no mínimo 10 caracteres.

## Casos de borda

- **Parâmetros de paginação fora do intervalo (`pagina < 1` ou `tamanhoPagina > 50`):** normalizar com limites seguros ou rejeitar com `HTTP 400` explicativo, impedindo consultas irrestritas ao banco.
- **CNPJ duplicado ou com quantidade de dígitos incorreta:** retornar `HTTP 400`/`422` em `ProblemDetails` em português sem gravar no banco.
- **Consulta, auditoria ou emissão para fornecedor/relatório/alerta inexistente:** retornar `HTTP 404` em `ProblemDetails` informando o identificador não localizado.
- **Acesso a rota de escrita sem token JWT ou com perfil insuficiente (ex.: `Analista` tentando aprovar auditoria restrita a `Auditor`):** retornar `HTTP 401` ou `HTTP 403`.

## Fora de escopo

- CRUD administrativo de manutenção/cadastro de usuários finais (conforme diretriz do desafio, credenciais corporativas de serviço/perfis são validadas apenas para emissão de token JWT).
- Interface gráfica web acoplada ao projeto .NET (o consumo visual ocorre via cliente externo/Postman/Simulador).
- Integração direta em tempo real com sensores físicos ou ERPs externos via mensageria assíncrona nesta etapa.

## Critérios de aceite

- Todos os 4 controllers expõem contratos REST claros e possuem testes automatizados em xUnit validando retorno `HTTP 200`.
- Listagens de fornecedores, emissões e alertas retornam metadados de paginação (`paginaAtual`, `tamanhoPagina`, `totalItens`, `totalPaginas`) e itens paginados.
- Cadastrar emissão que leve o fornecedor acima de 80% ou 100% da meta anual registra automaticamente o respectivo alerta aberto em `tb_alertas_conformidade`.
- Requisições inválidas retornam `ProblemDetails` em português; rotas críticas bloqueiam chamadas não autenticadas ou sem perfil adequado.
- Banco de dados inicializa estruturado por Migrations do Entity Framework Core e o projeto possui `Dockerfile` funcional.
