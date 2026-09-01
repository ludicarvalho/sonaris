# Sonaris

Aplicação de música para navegar e tocar a sua coleção de MP3. Composta por uma API com autenticação JWT e um frontend com player, rodando via Docker Compose.

## Stack

- **Backend**: ASP.NET Core 10 (Web API) — streaming de áudio com suporte a Range, leitura de metadados ID3v2/MPEG, extração de capa, edição de metadados e capa (grava ID3 v2.3 com capa em encoding Latin-1 via mutagen), busca full-text FTS5 híbrida (unicode61 + trigram), playlists persistidas em SQLite e scan automático de músicas em background.
- **Frontend**: React 19 + Vite + TypeScript + TailwindCSS — navegação por pastas, busca full-text, player com capa, volume, atalhos de teclado, layout responsivo, detalhes expansíveis com edição de metadados e capa, e sistema de playlists com criação/renomeação/exclusão/reordenação de faixas.
- **Infra**: Docker Compose (backend + nginx servindo o frontend), SQLite via bind mount.

## Estrutura

```
Sonaris/
├── Sonaris.sln            # Solução (backend + testes + frontend.esproj)
├── docker-compose.yml     # Orquestra backend + frontend
├── .env                   # Configurações (não versionado)
├── backend/               # API .NET 10 (Controllers, Services, parser de metadados)
│   ├── Services/Music/    # MusicMetadataReader/Writer (leitura e gravação ID3 via mutagen)
│   ├── Services/Search/   # Schema FTS5, MusicSearchService, MusicRepository, MusicFileScanner, MusicIndexerBackgroundService
│   ├── Services/Playlists/# PlaylistService (CRUD + reordenação)
│   └── Tests/             # Testes unitários (xUnit + Moq) — 146 testes
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
| `JWT_SECRET`       | *(obrigatório)*      | Segredo usado para assinar os tokens JWT (longo e aleatório) |
| `JWT_ISSUER`       | `sonaris`            | Emissor (iss) do token |
| `JWT_AUDIENCE`     | `sonaris`            | Audiência (aud) do token |
| `JWT_EXPIRA_EM_MINUTOS` | `1440`          | Validade do token em minutos |
| `ADMIN_USERNAME`   | `admin`              | Usuário administrador criado na primeira execução |
| `ADMIN_PASSWORD`   | `admin`              | Senha do administrador criado na primeira execução |
| `ADMIN_NOME`       | `Administrador`      | Nome de exibição do administrador |

A pasta de músicas é montada no container em `/Musicas` e pode ser **escrita** para permitir a edição de metadados pelo próprio Sonaris.

O banco SQLite é persistido em `~/sonaris/database/sonaris.db` via bind mount — os dados sobrevivem a `docker compose down` e `up`.

> O arquivo `.env` não é versionado. Alterações em `MUSIC_PATH`/portas exigem `docker compose up -d`; alterações em `VITE_API_URL` exigem rebuild: `docker compose up -d --build sonaris-frontend`. O `docker compose up` falha se `JWT_SECRET` não estiver definido (segurança).

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

### Autenticação (prefixo `/api/Auth`)

Todos os endpoints de música e playlist exigem um token JWT. Para obtê-lo, faça login:

| Ação         | Método | Endpoint                        |
| ------------ | ------ | ------------------------------- |
| Login        | POST   | `/api/Auth/Login` (body: `{ username, senha }`) → `{ token, user }` |
| Usuário atual | GET   | `/api/Auth/Me`                  |
| Listar usuários | GET | `/api/Auth/Usuarios` *(Admin)* |
| Registrar usuário | POST | `/api/Auth/Registrar` *(Admin)* |
| Alterar papel | PUT   | `/api/Auth/{id}/papel?isAdmin=<bool>` *(Admin)* |
| Alterar senha | PUT   | `/api/Auth/senha`               |

Envie o token no header `Authorization: Bearer <token>`. Endpoints que editam metadados (`EditarMetadados`) e os endpoints de Auth marcados como *(Admin)* exigem a role `Admin`. O streaming de áudio e capa recebem o token via query string (`?token=`), pois `<audio>`/`<img>` não conseguem enviar headers.

Na primeira execução, uma conta de administrador é criada automaticamente a partir das variáveis `ADMIN_USERNAME`/`ADMIN_PASSWORD`/`ADMIN_NOME`.

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

Cada usuário gerencia suas **próprias** playlists (isoladas por `user_id`). Playlists são referenciadas por `relativePath` — se uma música for renomeada/movida, a referência na playlist quebra (tradeoff aceito).

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

146 testes unitários cobrindo: schema FTS5, MusicSearchService, MusicMetadataReader,
MusicMetadataWriter, ArquivoService, PlaylistService, PlaylistController, MusicaController,
UserService, JwtTokenService e AuthController.

## Lint

```bash
cd frontend
npm run lint
```
