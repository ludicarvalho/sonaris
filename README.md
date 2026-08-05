# Sonaris

Aplicação de música para navegar e tocar a sua coleção de MP3. Composta por uma API simples (sem autenticação) e um frontend com player, rodando via Docker Compose.

## Stack

- **Backend**: ASP.NET Core 8 (Web API) — streaming de áudio com suporte a Range, leitura de metadados ID3v2/MPEG e extração de capa embutida.
- **Frontend**: React 19 + Vite + TypeScript + TailwindCSS — navegação por pastas, busca (scroll infinito) e player com capa, volume e atalhos de teclado.
- **Infra**: Docker Compose (backend + nginx servindo o frontend).

## Estrutura

```
Sonaris/
├── docker-compose.yml     # Orquestra backend + frontend
├── sonaris-backend/       # API .NET 8 (Controllers, Services, parser de metadados)
└── sonaris-frontend/      # React/Vite (página de músicas e player)
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

## Configuração da pasta de músicas

Por padrão o compose monta a pasta `/home/luiz/Músicas` como somente leitura dentro do container, em `/Musicas`.

Para usar outra pasta, edite o `docker-compose.yml` no serviço `sonaris-backend`:

```yaml
volumes:
  - /caminho/para/suas/musicas:/Musicas:ro
```

O backend lê a mesma variável `Settings__MusicPath` (valor padrão `/Musicas`).

> A coleção fica **somente leitura** (`:ro`) — o container nunca altera os arquivos.

## API

Endpoints disponíveis (prefixo `/api/Musica`):

| Ação         | Método | Endpoint                                                |
| ------------ | ------ | ------------------------------------------------------- |
| Listar pasta | GET    | `/api/Musica/BuscarMusicas?path=<pasta>&PageNumber=1&PageSize=30` |
| Stream       | GET    | `/api/Musica/StreamArquivo?fileName=<caminho.mp3>`      |
| Metadados    | GET    | `/api/Musica/BuscarMusicaMetadata?fileName=<caminho.mp3>` |
| Capa         | GET    | `/api/Musica/StreamCapa?fileName=<caminho.mp3>`         |

O endpoint de stream suporta requisições com header `Range` (respostas HTTP 206).

### Apontando o frontend para outra API

A URL da API é definida no build do frontend pelo argumento `VITE_API_URL`. Para alterar, edite o `docker-compose.yml`:

```yaml
args:
  - VITE_API_URL=http://localhost:5033
```

e recrie o container (`docker compose up -d --build sonaris-frontend`).

## Desenvolvimento local

### Frontend

```bash
cd sonaris-frontend
npm install
npm run dev      # Vite em http://localhost:5173
```

Configure a API em `.env` (crie a partir do `.env.example`):

```
VITE_API_URL=http://localhost:5033
```

### Backend

```bash
cd sonaris-backend
dotnet restore
dotnet run --project Sonaris.Backend.csproj
```

Defina a pasta de músicas em `appsettings.json` (`Settings:MusicPath`).

## Lint

```bash
cd sonaris-frontend
npm run lint
```
