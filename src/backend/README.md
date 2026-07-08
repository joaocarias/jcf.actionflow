# jcf-actionflow — Backend

API para visualizar e editar skills de Actions do IBM Watson Assistant: importa o JSON
exportado do Watson, expõe leitura de collections/actions/steps, monta o grafo do fluxo
para o front (React Flow), copia/move actions entre collections, valida a integridade do
workspace e reexporta um JSON pronto para reimportar no Watson.

## Projetos

- **Jcf.ActionFlow.Core** — modelos do domínio, parser/serializador, construção do grafo,
  operação de cópia/move, validador. Sem dependência de ASP.NET; testável isoladamente.
- **Jcf.ActionFlow.Api** — endpoints HTTP (minimal APIs), upload/download do JSON, CORS,
  ProblemDetails, Swagger/Scalar em dev.
- **Jcf.ActionFlow.Tests** — xUnit, cobre o Core e um smoke test HTTP fim a fim via
  `WebApplicationFactory`.

## Rodando localmente

```bash
cd src/backend
dotnet build
dotnet run --project Jcf.ActionFlow.Api
```

A API sobe em `http://localhost:5000` (ou na porta do `launchSettings.json`/`ASPNETCORE_URLS`).
Em `Development`, a documentação interativa (Scalar) fica em `/scalar`.

## Rodando via Docker

```bash
docker compose up --build api
```

Isso builda `Jcf.ActionFlow.Api/Dockerfile` (contexto `src/backend`, então o `Core` também
entra na imagem) e publica em `API_PORT` (ver `.env`).

## Testes

```bash
cd src/backend
dotnet test
```

Os testes usam `samples/Lab-Chat-action.json` (na raiz do repo) como fixture real — um
export genuíno do Watson, copiado para o output de teste via `Content/Link` no `.csproj`.

## Fluxo completo via curl

Todos os exemplos abaixo foram rodados de verdade contra a API em Docker.

### 1. Upload

```bash
curl -sS -F "file=@samples/Lab-Chat-action.json;type=application/json" \
  http://localhost:5000/api/workspaces
```

```json
{
  "sessionId": "8680d09a44d34d13affd695ea831f5ad",
  "summary": {
    "sessionId": "8680d09a44d34d13affd695ea831f5ad",
    "name": "Lab-Chat-action",
    "actionCount": 9,
    "businessActionCount": 5,
    "systemActionCount": 4,
    "intentCount": 6,
    "variableCount": 8,
    "collectionCount": 2
  },
  "issues": []
}
```

Guarde o `sessionId` — todo o resto das chamadas usa `$SESSION_ID`.

### 2. Grafo (nível actions)

```bash
curl -sS "http://localhost:5000/api/workspaces/$SESSION_ID/graph?level=actions"
```

9 nós (um por action, agrupados por collection; as 4 de sistema no grupo `"system"`) e 13
arestas (5 `invoke` + 8 `ordering`, incluindo o loop `action_25692 ↔ action_8197`). Troque
para `?level=steps` para o grafo detalhado por step (23 nós, 19 arestas na fixture).

### 3. Copiar uma action para outra collection

```bash
curl -sS -X POST "http://localhost:5000/api/workspaces/$SESSION_ID/actions/action_49668/copy" \
  -H "Content-Type: application/json" \
  -d '{"targetCollection":"prod","mode":"copy","titlePrefix":"prod/","referenceStrategy":"keep"}'
```

Clona `action_49668` → `action_49668-3` (o sufixo `-2` já estava em uso pela
`action_49668-2` do próprio arquivo de exemplo), clona o intent, troca o título para
`prod/triagem/finaliza` e mantém `next_action` apontando para `action_8197` (que continua
em `hml`) — e por isso devolve um warning avisando que essa referência agora cruza
collections:

```json
{
  "action": { "action": "action_49668-3", "title": "prod/triagem/finaliza", "...": "..." },
  "warnings": [
    "action_49668-3.next_action: referência para 'action_8197' (collection 'collection_45544') mantida fora da collection destino 'collection_4598'."
  ],
  "issues": []
}
```

Use `"mode":"move"` para apenas realocar a `action_reference` (sem clonar nada), ou
`"referenceStrategy":"remap"` para tentar reapontar referências para uma action já
existente na collection destino com o mesmo título (trocando o prefixo de ambiente).

### 4. Validar

```bash
curl -sS "http://localhost:5000/api/workspaces/$SESSION_ID/validate"
```

`[]` no arquivo de exemplo (sem defeitos). Injete um defeito (ex.: apague uma
`action_reference` válida) para ver os issues aparecerem.

### 5. Exportar

```bash
curl -sS "http://localhost:5000/api/workspaces/$SESSION_ID/export" -o export.json
```

JSON completo, pronto para reimportar no Watson — inclusive a action clonada no passo 3.

## Decisões de design

- **.NET 10, não .NET 8.** O restante do repositório (Dockerfiles, `docker-compose.yml`)
  já estava montado em cima do SDK 10 antes deste backend existir; manter a mesma versão
  evitou downgrade de infra só por causa do texto do prompt.
- **Round-trip via `[JsonExtensionData]` em todo modelo**, em vez de modelar o schema
  inteiro do Watson. Só os campos que o sistema efetivamente lê/edita (actions, steps,
  resolvers, condition, collections, intents, variables) são tipados; todo o resto
  (`boosts`, `topic_switch`, `system_settings`, formato rico de `output`/`question`, etc.)
  passa incólume. Comprovado por teste contra o export real.
- **`Condition` unificado para action e step.** Os três formatos comuns
  (`{"intent": ...}`, `{"expression": ...}`, `{"entity": ...}`) são tipados; os operadores
  de comparação (`neq`, `gte`, `lt`, `lte`, `gt`, `eq`, ...), por terem chave dinâmica, caem
  no `ExtensionData` da própria classe — preservados no round-trip e ainda disponíveis para
  o `ConditionFormatter` renderizar como label de aresta/mensagem de validação.
- **Detecção de variável não declarada é um scan por regex sobre o JSON do step**, não uma
  modelagem exaustiva de todo lugar onde uma variável pode aparecer (saída de texto,
  condições, atribuições de contexto, `result_variable`, `${step_X}` em expressões). O scan
  procura as chaves `"variable"`, `"result_variable"`, `"skill_variable"` e o padrão
  `${nome}`, ignorando propositalmente `"system_variable"` (built-ins do Watson como
  `fallback_reason`, que nunca são declaradas em `variables[]`).
- **Grafo de steps usa nós namespaced `"{actionId}::{stepId}"`.** Ids de step se repetem
  entre actions (ex.: `step_798` existe em `action_49668` e em `action_49668-2`), então o
  id da action entra na chave para não colidir.
- **Aresta de invoke no grafo de steps aponta para o primeiro step da action alvo**
  (`Steps[0]`), assumindo esse como o ponto de entrada — o Watson não tem um marcador
  explícito de "step inicial", só a ordem do array.
- **Condição do step vira label da(s) aresta(s) de saída daquele step**, em vez de virar um
  terceiro tipo de aresta. Um step com `next_step` *e* `invoke_another_action` (existe um
  caso assim na fixture: `step_440`) tem as duas arestas rotuladas com a mesma condição.
- **`targetCollection` no copy/move aceita id OU título** (`"collection_4598"` ou
  `"prod"`) — mais ergonômico para quem está testando via curl, sem custo extra.
- **`move` não clona nada.** Só realoca a `action_reference`; a action e o intent
  permanecem exatamente os mesmos objetos (é assim que o prompt descreve o comportamento —
  bem diferente de `copy`, que clona tudo).
- **Sem banco de dados.** `IWorkspaceRepository` + `InMemoryWorkspaceRepository`
  (thread-safe via `ConcurrentDictionary`); sessões somem ao reiniciar o processo. A
  interface já está pronta para uma implementação persistente entrar sem tocar no resto do
  Core.
- **CORS liberado geral** (`AllowAnyOrigin/Method/Header`) — não há autenticação nem
  isolamento multi-tenant ainda; cada operação de escrita já é isolada por `sessionId`
  opaco. Precisa apertar antes de sair da fase 1.

## O que fica para a fase 2

- Persistência real (trocar `InMemoryWorkspaceRepository` por algo com storage).
- Autenticação/autorização.
- Edição de steps (hoje só há rename/delete de action — criar, editar e reordenar steps,
  editar `output`/`question`/`context` tipados em vez de crus, ainda não existe).
- Undo/histórico de edições dentro de uma sessão.
- Validação de schema mais rígida no upload (hoje só checa se existe `workspace.actions`
  não vazio).
- `remap` só casa por título exato após troca de prefixo; não lida com collections cujo
  título não seja um prefixo de ambiente simples (`hml/`, `prod/`, ...).
