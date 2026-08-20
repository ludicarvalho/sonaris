# Sonaris

Aplicação de música para navegar e tocar a sua coleção de MP3. Composta por uma API (sem autenticação) e um frontend com player, rodando via Docker Compose.

## Stack

- **Backend**: ASP.NET Core 10 (Web API) — streaming de áudio com suporte a Range, leitura de metadados ID3v2/MPEG, extração de capa, busca full-text FTS5 híbrida (unicode61 + trigram), playlists persistidas em SQLite e scan automático de músicas em background.
- **Frontend**: React 19 + Vite + TypeScript + TailwindCSS — navegação por pastas, busca full-text, player com capa, volume, atalhos de teclado, layout responsivo, detalhes expansíveis com edição de metadados e capa, e sistema de playlists com criação/renomeação/exclusão/reordenação de faixas.
- **Infra**: Docker Compose (backend + nginx servindo o frontend), SQLite via bind mount.

## Estrutura

```
Sonaris/
├── Sonaris.sln            # Solução (backend + testes + frontend.esproj)
├── docker-compose.yml     # Orquestra backend + frontend
├── .env                   # Configurações (não versionado)
├── backend/               # API .NET 10 (Controllers, Services, parser de metadados)
│   ├── Services/Search/   # Schema FTS5, MusicSearchService, MusicIndexerBackgroundService
│   ├── Services/Playlists/# PlaylistService (CRUD + reordenação)
│   └── Tests/             # Testes unitários (xUnit + Moq) — 99 testes
└── frontend/              # React/Vite (página de músicas, player e playlists)
```

## Requisitos

- [Docker](https://docs.docker.com/engine/install/) com Docker Compose v2.

## Como executar

Na raiz do projeto:

```bash
docker compose up -d --build
```

Após subir:

| Serviço     | URL                                   |
| ----------- | ------------------------------------- |
| Frontend    | http://localhost:3003                 |
| Backend     | http://localhost:5033                 |
| Swagger API | http://localhost:5033/swagger         |

Para derrubar:

```bash
docker compose down
```

## Configuração (arquivo `.env`)

As configurações ficam em um `.env` na **raiz do projeto** (o Docker Compose lê automaticamente). Crie a partir do exemplo:

```bash
cp .env.example .env
```

| Variável           | Padrão              | Descrição                                             |
| ------------------ | ------------------- | ----------------------------------------------------- |
| `VITE_API_URL`     | *(vazio)*           | URL da API. **Vazio = mesma origem** (nginx faz proxy do `/api` para o backend) |
| `MUSIC_PATH`       | `/home/luiz/Músicas` | Pasta no host com a coleção de MP3 |
| `BACKEND_PORT`     | `5033`              | Porta do host para a API |
| `FRONTEND_PORT`    | `3003`              | Porta do host para o frontend |
| `SONARIS_DATA_DIR` | `~/sonaris/database` | Diretório no host para persistir o banco SQLite |

A pasta de músicas é montada no container em `/Musicas` e pode ser **escrita** para permitir a edição de metadados pelo próprio Sonaris.

O banco SQLite é persistido em `~/sonaris/database/sonaris.db` via bind mount — os dados sobrevivem a `docker compose down` e `up`.

> O arquivo `.env` não é versionado. Alterações em `MUSIC_PATH`/portas exigem `docker compose up -d`; alterações em `VITE_API_URL` exigem rebuild: `docker compose up -d --build sonaris-frontend`.

## API

### Músicas (prefixo `/api/Musica`)

| Ação         | Método | Endpoint                                                |
| ------------ | ------ | ------------------------------------------------------- |
| Listar pasta | GET    | `/api/Musica/BuscarMusicas?path=<pasta>&PageNumber=1&PageSize=30` |
| Stream       | GET    | `/api/Musica/StreamArquivo?fileName=<caminho.mp3>`      |
| Metadados    | GET    | `/api/Musica/BuscarMusicaMetadata?fileName=<caminho.mp3>` |
| Capa         | GET    | `/api/Musica/StreamCapa?fileName=<caminho.mp3>`         |
| Editar metadados | POST   | `/api/Musica/EditarMetadados` (multipart) |
| Busca FTS5   | GET    | `/api/Musica/BuscarFullText?termo=<termo>`              |

A busca full-text usa um índice híbrido SQLite FTS5:
- `music_fts` (unicode61) — busca por título, artista, álbum e filename
- `music_path_fts` (trigram) — busca por caminho relativo (substrings)

Resultados duplicados de ambas as tabelas são deduplicados via CTEs com `ROW_NUMBER()`.

O indexer roda automaticamente no startup e a cada 5 minutos, indexando todas as músicas encontradas no diretório configurado.

### Playlists (prefixo `/api/Playlist`)

| Ação             | Método | Endpoint                                      |
| ---------------- | ------ | --------------------------------------------- |
| Listar           | GET    | `/api/Playlist`                               |
| Criar            | POST   | `/api/Playlist` (body: nome)                  |
| Renomear         | PUT    | `/api/Playlist/{id}?novoNome=<nome>`          |
| Deletar          | DELETE | `/api/Playlist/{id}`                          |
| Detalhes         | GET    | `/api/Playlist/{id}`                          |
| Adicionar faixa  | POST   | `/api/Playlist/{id}/tracks` (body: relativePath) |
| Remover faixa    | DELETE | `/api/Playlist/{id}/tracks/{trackId}`         |
| Reordenar faixa  | PUT    | `/api/Playlist/{id}/tracks/{trackId}/reorder` |
| Duplicar         | POST   | `/api/Playlist/{id}/duplicate?novoNome=<nome>` |

Playlists são referenciadas por `relativePath` — se uma música for renomeada/movida, a referência na playlist quebra (tradeoff aceito).

Nomes duplicados são impedidos pelo backend (tanto em criação quanto renomeação).

### Capa da música

A capa é buscada na ordem:

1. **Imagem embutida** na tag ID3v2 da própria música.
2. Se não houver, uma **imagem no mesmo diretório** — a primeira encontrada entre `.jpg`, `.jpeg` e `.png` (ex.: `folder.jpg`).

### Como a API é acessada

O nginx do container do frontend faz **proxy** de todas as chamadas `/api/*` para o backend (serviço `sonaris-backend:7071`). O `VITE_API_URL` fica **vazio** por padrão: o browser chama a mesma origem do frontend. O backend também continua exposto na porta do host (`BACKEND_PORT`) para acesso direto (Swagger, testes).

## Desenvolvimento local

### Visual Studio (F5 — backend + frontend juntos)

O `Sonaris.sln` já inclui o `frontend/frontend.esproj`, então o F5 consegue rodar os dois. Para isso:

1. Abra o `Sonaris.sln` no Visual Studio 2022.
2. **Configure Startup Projects**: clique com o botão direito na solução → *Configure Startup Projects…* → escolha **Multiple startup projects** → defina **`Sonaris.Backend`** e **`frontend`** como **Start** (nessa ordem).
3. Aperte **F5**. O backend sobe em `http://localhost:7071` e o frontend (Vite) em `http://localhost:5174` — abra esse endereço no navegador.

> Esse ajuste é uma configuração local do VS (fica em `.vs/`, não versionado) — só precisa ser feito uma vez.

O Vite tem um **proxy** de `/api/*` para o backend em `http://localhost:7071` (configurado em `frontend/vite.config.ts`).

### Frontend (só o front, sem o VS)

```bash
cd frontend
npm install
npm run dev      # Vite em http://localhost:5174
```

### Backend

```bash
cd backend
dotnet restore
dotnet run --project Sonaris.Backend.csproj
```

## Testes

```bash
cd backend
dotnet test
```

99 testes unitários cobrindo: schema FTS5, MusicSearchService, PlaylistService, PlaylistController e MusicaController.

## Lint

```bash
cd frontend
npm run lint
```
