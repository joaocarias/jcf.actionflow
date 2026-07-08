# jcf.actionflow — ActionFlow

Ferramenta web para importar, visualizar e editar skills de **Actions do IBM Watson
Assistant**. Você sobe o JSON exportado do Watson, navega pelas collections, actions e
steps (inclusive em grafo), copia/move actions entre collections e reexporta um JSON
pronto para reimportar no Watson — sem perder nenhum campo pelo caminho.

## Status

Funcional ponta a ponta para o fluxo principal: upload → navegar → editar → exportar.

| Área | Status |
|---|---|
| Import/export do JSON (round-trip sem perdas) | ✅ |
| Navegar collections, actions e actions de sistema/órfãs | ✅ |
| Detalhe de uma action com seus steps (condição, output, invoke) | ✅ |
| Copiar/mover action entre collections (`keep`/`remap`) | ✅ |
| Renomear e excluir action (com bloqueio por referências) | ✅ |
| Validador de integridade (referências quebradas, variáveis não declaradas, ...) | ✅ |
| Visualização em grafo (actions e steps, via React Flow + elkjs) | ✅ |
| Edição de steps individualmente | ⏳ fase 2 |
| Persistência (hoje é tudo em memória, por sessão) | ⏳ fase 2 |
| Autenticação | ⏳ fase 2 |

## Stack

- **Backend**: .NET 10, ASP.NET Core (minimal APIs), `System.Text.Json`. Sem banco de
  dados por enquanto — cada workspace importado vira uma sessão em memória.
- **Frontend**: React 19 + TypeScript + Vite + Tailwind CSS v4, `react-router-dom`,
  `@xyflow/react` (React Flow) + `elkjs` para o grafo.
- **Docker**: um `Dockerfile` por projeto + `docker-compose.yml` na raiz subindo os dois.

## Estrutura

```
src/
  backend/
    Jcf.ActionFlow.slnx
    Jcf.ActionFlow.Core/    # modelos, parser, grafo, cópia/move, validador
    Jcf.ActionFlow.Api/     # endpoints HTTP
    Jcf.ActionFlow.Tests/   # xUnit (32 testes)
    README.md               # workflow via curl + decisões de design do backend
  frontend/
    jcf-actionflow-app/     # app React
samples/
  Lab-Chat-action.json      # export real do Watson, usado como fixture dos testes
docker-compose.yml
```

## Rodando

### Docker (mais simples)

```bash
docker compose up --build
```

- API em `http://localhost:5000`
- Front em `http://localhost:5173`

Portas e URLs configuráveis via `.env` (copie de `.env.example`).

### Local, sem Docker

```bash
# backend
cd src/backend
dotnet run --project Jcf.ActionFlow.Api   # http://localhost:5000, /scalar para docs

# frontend (em outro terminal)
cd src/frontend/jcf-actionflow-app
npm install
npm run dev                                # http://localhost:5173
```

### Testes

```bash
cd src/backend
dotnet test
```

Veja `src/backend/README.md` para o passo a passo completo via `curl` (upload → grafo →
copiar → validar → exportar) e as decisões de design do modelo de domínio. Veja
`CLAUDE.md` para comandos de build/lint do dia a dia.
