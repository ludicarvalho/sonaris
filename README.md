# Sonaris

Aplicação de música para navegar e tocar a sua coleção de MP3. Composta por uma API simples (sem autenticação) e um frontend com player, rodando via Docker Compose.

## Stack

- **Backend**: ASP.NET Core 8 (Web API) — streaming de áudio com suporte a Range, leitura de metadados ID3v2/MPEG e extração de capa (embutida na ID3v2 ou imagem no diretório).
- **Frontend**: React 19 + Vite + TypeScript + TailwindCSS — navegação por pastas, busca (scroll infinito) e player com capa (embutida ou imagem da pasta), volume, atalhos de teclado, layout responsivo e detalhes expansíveis (o botão Voltar do navegador fecha os detalhes no mobile).
- **Infra**: Docker Compose (backend + nginx servindo o frontend).

## Estrutura

```
Sonaris/
├── Sonaris.sln            # Solução .NET (backend + testes)
├── docker-compose.yml     # Orquestra backend + frontend
├── sonaris-backend/       # API .NET 8 (Controllers, Services, parser de metadados)
│   └── Tests/             # Projeto de testes (xunit + Moq)
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

## Configuração (arquivo `.env`)

As configurações ficam em um `.env` na **raiz do projeto** (o Docker Compose lê automaticamente). Crie a partir do exemplo:

```bash
cp .env.example .env
```

| Variável       | Padrão              | Descrição                                             |
| -------------- | ------------------- | ----------------------------------------------------- |
| `VITE_API_URL` | *(vazio)*           | URL da API. **Vazio = mesma origem** (o nginx do frontend faz proxy do `/api` para o backend) |
| `MUSIC_PATH`   | `/home/luiz/Músicas` | Pasta no host com a coleção de MP3 |
| `BACKEND_PORT` | `5033`              | Porta do host para a API |
| `FRONTEND_PORT`| `3003`              | Porta do host para o frontend |

A pasta de músicas é montada no container em `/Musicas` e pode ser **escrita** para permitir a edição de metadados pelo próprio Sonaris (o container altera apenas as tags ID3/capa dos MP3).

> O arquivo `.env` não é versionado. Alterações em `MUSIC_PATH`/portas exigem `docker compose up -d`; alterações em `VITE_API_URL` exigem rebuild: `docker compose up -d --build sonaris-frontend`.

## API

Endpoints disponíveis (prefixo `/api/Musica`):

| Ação         | Método | Endpoint                                                |
| ------------ | ------ | ------------------------------------------------------- |
| Listar pasta | GET    | `/api/Musica/BuscarMusicas?path=<pasta>&PageNumber=1&PageSize=30` |
| Stream       | GET    | `/api/Musica/StreamArquivo?fileName=<caminho.mp3>`      |
| Metadados    | GET    | `/api/Musica/BuscarMusicaMetadata?fileName=<caminho.mp3>` |
| Capa         | GET    | `/api/Musica/StreamCapa?fileName=<caminho.mp3>`         |
| Editar metadados | POST   | `/api/Musica/EditarMetadados` (multipart: `fileName`, `title`, `artist`, `album`, `track`, `year`, `removerCapa`, `capa`) |

A edição de metadados usa as ferramentas `mid3v2` (campos de texto) e `eyeD3` (capa embutida/APIC), instaladas na imagem do backend — os mesmos utilitários usados manualmente no host. Durante a gravação o player pausa e retoma automaticamente.

O endpoint de stream suporta requisições com header `Range` (respostas HTTP 206).

### Capa da música

A capa é buscada na ordem:

1. **Imagem embutida** na tag ID3v2 da própria música.
2. Se não houver, uma **imagem no mesmo diretório** da música — a primeira encontrada entre `.jpg`, `.jpeg` e `.png` (ex.: `folder.jpg`).

Se nenhuma imagem for encontrada, o endpoint retorna erro 400 (`Capa não encontrada`).

### Como a API é acessada

O nginx do container do frontend faz **proxy** de todas as chamadas `/api/*` para o backend (serviço `sonaris-backend:7071`). Por isso o `VITE_API_URL` fica **vazio** por padrão: o browser chama a mesma origem do frontend e funciona em qualquer máquina, sem configurar IP. O backend também continua exposto na porta do host (`BACKEND_PORT`) para acesso direto (Swagger, testes).

Só use uma URL completa no `VITE_API_URL` se estiver servindo o frontend sem o proxy (ex.: Vite em dev, apontando direto para a API).

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
