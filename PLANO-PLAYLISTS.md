# Plano: Playlists + SQLite FTS5 no Sonaris

## Visão Geral

Duas features grandes, integradas:

| Feature | Onde | Persistência |
|---|---|---|
| **SQLite FTS5** — busca full-text por título, artista, álbum, filename | Backend | `sonaris.db` (SQLite) |
| **Playlists** — criar, editar, tocar, shuffle, repeat | Backend + Frontend | `sonaris.db` (SQLite) |

---

## PARTE 1: SQLite FTS5 (Backend)

### 1.1 Pacote NuGet

```bash
dotnet add package Microsoft.Data.Sqlite
```

Sem EF Core. ADO.NET puro, leve, sem overhead.

### 1.2 Schema do Banco

```sql
-- Tabela principal de músicas indexadas
CREATE TABLE IF NOT EXISTS music (
    id              INTEGER PRIMARY KEY AUTOINCREMENT,
    title           TEXT NOT NULL DEFAULT '',
    artist          TEXT NOT NULL DEFAULT '',
    album           TEXT NOT NULL DEFAULT '',
    track           TEXT NOT NULL DEFAULT '',
    year            TEXT NOT NULL DEFAULT '',
    duration_secs   REAL,
    bitrate         INTEGER,
    filename        TEXT NOT NULL,
    relative_path   TEXT NOT NULL UNIQUE,
    file_size       INTEGER NOT NULL DEFAULT 0,
    last_modified   TEXT NOT NULL DEFAULT '',
    last_scanned    TEXT NOT NULL DEFAULT ''
);

-- Índice FTS5 padrão (unicode61) — busca por palavras em metadados
CREATE VIRTUAL TABLE IF NOT EXISTS music_fts USING fts5(
    title, artist, album,
    content='music',
    content_rowid='id'
);

-- Índice FTS5 trigram — busca por substring em filename/caminho
CREATE VIRTUAL TABLE IF NOT EXISTS music_path_fts USING fts5(
    filename, relative_path,
    tokenize='trigram',
    detail='none'
);

-- Triggers de sincronização automática (music_fts)
CREATE TRIGGER IF NOT EXISTS music_fts_insert AFTER INSERT ON music BEGIN
    INSERT INTO music_fts(rowid, title, artist, album)
    VALUES (new.id, new.title, new.artist, new.album);
END;

CREATE TRIGGER IF NOT EXISTS music_fts_update AFTER UPDATE ON music BEGIN
    INSERT INTO music_fts(music_fts, rowid, title, artist, album)
    VALUES ('delete', old.id, old.title, old.artist, old.album);
    INSERT INTO music_fts(rowid, title, artist, album)
    VALUES (new.id, new.title, new.artist, new.album);
END;

CREATE TRIGGER IF NOT EXISTS music_fts_delete AFTER DELETE ON music BEGIN
    INSERT INTO music_fts(music_fts, rowid, title, artist, album)
    VALUES ('delete', old.id, old.title, old.artist, old.album);
END;

-- Triggers de sincronização automática (music_path_fts)
CREATE TRIGGER IF NOT EXISTS music_path_fts_insert AFTER INSERT ON music BEGIN
    INSERT INTO music_path_fts(rowid, filename, relative_path)
    VALUES (new.id, new.filename, new.relative_path);
END;

CREATE TRIGGER IF NOT EXISTS music_path_fts_update AFTER UPDATE ON music BEGIN
    INSERT INTO music_path_fts(music_path_fts, rowid, filename, relative_path)
    VALUES ('delete', old.id, old.filename, old.relative_path);
    INSERT INTO music_path_fts(rowid, filename, relative_path)
    VALUES (new.id, new.filename, new.relative_path);
END;

CREATE TRIGGER IF NOT EXISTS music_path_fts_delete AFTER DELETE ON music BEGIN
    INSERT INTO music_path_fts(music_path_fts, rowid, filename, relative_path)
    VALUES ('delete', old.id, old.filename, old.relative_path);
END;
```

### 1.3 Background Scanner

Um `BackgroundService` que roda ao iniciar o app:

```csharp
// Services/Search/MusicIndexerBackgroundService.cs
public class MusicIndexerBackgroundService : BackgroundService
{
    // 1. Varre todo o MUSIC_PATH recursivamente por .mp3
    // 2. Para cada arquivo, lê metadados via IMusicMetadataReader
    // 3. Faz upsert no SQLite (ON CONFLICT relative_path DO UPDATE)
    // 4. Remove do banco arquivos que não existem mais no filesystem
    // 5. Rode em background, não bloqueia a inicialização da API
    // 6. Intervalo: scan inicial ao startup + re-scan a cada 5 minutos
}
```

### 1.4 Serviço de Busca

```csharp
// Services/Search/IMusicSearchService.cs
public interface IMusicSearchService
{
    /// <summary>
    /// Busca híbrida: FTS5 padrão (metadados) + trigram (filename/caminho).
    /// Resultados combinados e ranqueados por BM25.
    /// </summary>
    PagedResult<MusicSearchResult> Search(string query, int pageNumber = 1, int pageSize = 30);
    Task<int> GetIndexedCountAsync();
}
```

**Busca híbrida — FTS5 padrão + trigram:**

```sql
-- Busca em metadados (palavras inteiras, BM25 ranking)
SELECT m.*, fts.rank AS rank,
       snippet(music_fts, 0, '<b>', '</b>', '...', 32) AS snippet,
       'metadata' AS match_source
FROM music_fts fts
JOIN music m ON m.id = fts.rowid
WHERE music_fts MATCH @query

UNION ALL

-- Busca em filename/caminho (substring via trigram)
SELECT m.*, 0 AS rank,
       '' AS snippet,
       'path' AS match_source
FROM music_path_fts fts
JOIN music m ON m.id = fts.rowid
WHERE music_path_fts MATCH '"' || @query || '"'

ORDER BY rank
LIMIT @limit OFFSET @offset
```

**Exemplo — busca por "pai":**

| Resultado | match_source | Rank |
|---|---|---|
| Música: "Meu **Pai**" | `metadata` | BM25 (alto) |
| Música: "O **Pai** da Música" | `metadata` | BM25 (médio) |
| Arquivo: `rock/**pai**xao.mp3` | `path` | 0 (substituto) |
| Arquivo: `artistas/**pai**nos.mp3` | `path` | 0 (substituto) |

Metadados sempre vêm primeiro (BM25 > 0). Path serve como fallback para filenames.

### 1.5 Novo Endpoint

```
GET /api/Musica/BuscarFullText?query=beatles&PageNumber=1&PageSize=30
```

Retorna resultados ranqueados por relevância com BM25 + snippet com highlight.

### 1.6 Arquivos Novos (Backend)

```
backend/
├── Domain/
│   └── Entities/
│       └── MusicFileEntry.cs          ← NOVO
│   └── DTOs/Music/
│       └── MusicSearchResult.cs       ← NOVO
├── Services/
│   └── Search/
│       ├── IMusicSearchService.cs     ← NOVO
│       ├── MusicSearchService.cs      ← NOVO
│       └── MusicIndexerBackgroundService.cs ← NOVO
└── Program.cs                         ← MODIFICADO (DI + startup scan)
```

---

## PARTE 2: Playlists (Backend SQLite)

### 2.1 Schema das Playlists

```sql
-- Tabela de playlists
CREATE TABLE IF NOT EXISTS playlist (
    id          TEXT PRIMARY KEY,       -- GUID
    name        TEXT NOT NULL,
    created_at  TEXT NOT NULL,
    updated_at  TEXT NOT NULL
);

-- Tabela de faixas da playlist (com ordem)
CREATE TABLE IF NOT EXISTS playlist_track (
    id              INTEGER PRIMARY KEY AUTOINCREMENT,
    playlist_id     TEXT NOT NULL REFERENCES playlist(id) ON DELETE CASCADE,
    relative_path   TEXT NOT NULL,
    title           TEXT NOT NULL DEFAULT '',
    artist          TEXT NOT NULL DEFAULT '',
    album           TEXT NOT NULL DEFAULT '',
    position        INTEGER NOT NULL DEFAULT 0,
    added_at        TEXT NOT NULL
);

CREATE INDEX IF NOT EXISTS idx_playlist_track_playlist_id
    ON playlist_track(playlist_id, position);
```

### 2.2 Serviço de Playlists

```csharp
// Services/Playlists/IPlaylistService.cs
public interface IPlaylistService
{
    List<PlaylistDto> GetAll();
    PlaylistDto? GetById(string id);
    PlaylistDto Create(string name);
    PlaylistDto Rename(string id, string name);
    void Delete(string id);
    PlaylistTrackDto AddTrack(string playlistId, string relativePath);
    void RemoveTrack(string playlistId, long trackId);
    void ReorderTrack(string playlistId, long trackId, int newPosition);
    void Duplicate(string id, string newName);
}
```

### 2.3 Endpoints

```
GET    /api/Playlist                    → Listar todas
GET    /api/Playlist/{id}               → Buscar por ID (com faixas)
POST   /api/Playlist                    → Criar { name }
PUT    /api/Playlist/{id}               → Renomear { name }
DELETE /api/Playlist/{id}               → Excluir
POST   /api/Playlist/{id}/tracks        → Adicionar faixa { relativePath }
DELETE /api/Playlist/{id}/tracks/{trackId} → Remover faixa
PUT    /api/Playlist/{id}/tracks/reorder → Reordenar { trackId, newPosition }
POST   /api/Playlist/{id}/duplicate     → Duplicar { name }
```

### 2.4 Controller

```csharp
// Controllers/PlaylistController.cs
[Route("api/Playlist")]
public class PlaylistController(IPlaylistService playlistService) : BaseController
{
    // GET, POST, PUT, DELETE — endpoints listados acima
}
```

### 2.5 Arquivos Novos (Backend)

```
backend/
├── Domain/
│   └── DTOs/Playlist/
│       ├── PlaylistDto.cs             ← NOVO
│       └── PlaylistTrackDto.cs        ← NOVO
├── Services/
│   └── Playlists/
│       ├── IPlaylistService.cs        ← NOVO
│       └── PlaylistService.cs         ← NOVO
└── Controllers/
    └── PlaylistController.cs          ← NOVO
```

---

## PARTE 3: Frontend — Playlists

### 3.1 Serviço HTTP

```typescript
// services/playlist.service.ts
export const playlistService = {
    listar: () => http.get<Playlist[]>('/api/Playlist'),
    buscar: (id: string) => http.get<Playlist>(`/api/Playlist/${id}`),
    criar: (name: string) => http.post<Playlist>('/api/Playlist', { name }),
    renomear: (id: string, name: string) => http.put(`/api/Playlist/${id}`, { name }),
    excluir: (id: string) => http.delete(`/api/Playlist/${id}`),
    adicionarFaixa: (id: string, relativePath: string) =>
        http.post(`/api/Playlist/${id}/tracks`, { relativePath }),
    removerFaixa: (id: string, trackId: number) =>
        http.delete(`/api/Playlist/${id}/tracks/${trackId}`),
    reordenar: (id: string, trackId: number, position: number) =>
        http.put(`/api/Playlist/${id}/tracks/reorder`, { trackId, position }),
};
```

### 3.2 Contexto de Playlists

```typescript
// contexts/PlaylistContext.tsx
interface PlaylistContextType {
    playlists: Playlist[];
    playlistAtual: Playlist | null;
    indiceAtual: number;
    modoRepeat: 'off' | 'all' | 'one';
    shuffle: boolean;
    fila: PlaylistTrack[];  // ordem efetiva (com shuffle)

    tocarPlaylist: (playlist: Playlist, startIndex?: number) => void;
    tocarProxima: () => void;
    tocarAnterior: () => void;
    alternarShuffle: () => void;
    alternarRepeat: () => void;
    adicionarAFila: (track: PlaylistTrack) => void;
    removerDaFila: (trackId: number) => void;
    refreshPlaylists: () => Promise<void>;
}
```

### 3.3 Componentes Novos

| Componente | Função |
|---|---|
| `PainelPlaylist.tsx` | Drawer lateral com lista de playlists |
| `ListaPlaylist.tsx` | Lista de faixas de uma playlist (drag-to-reorder) |
| `CriarPlaylistDialog.tsx` | Modal para criar/renomear |
| `MenuAdicionarPlaylist.tsx` | Dropdown no player para adicionar à playlist |
| `ShuffleRepeatControles.tsx` | Botões shuffle/repeat no player |

### 3.4 Modificações nos Existentes

| Arquivo | Mudança |
|---|---|
| `Musicas.tsx` | Envolver com `PlaylistProvider`, toggle do painel |
| `PlayerMusica.tsx` | Integrar com `PlaylistContext` para prev/next da fila |
| `BlocoCentral.tsx` | Adicionar botões shuffle/repeat |
| `BlocoDireita.tsx` | Adicionar botão "Playlist" |
| `ListaMusicas.tsx` | Botão "Adicionar à playlist" em cada faixa |
| `usePlayerAudio.ts` | `onEnded` respeita repeat mode |
| `BuscadorMusicas.tsx` | Chamar FTS5 endpoint, mostrar mais info (artista/álbum) |
| `types.ts` | Adicionar `Playlist`, `PlaylistTrack` |

### 3.5 Estrutura Final

```
src/
├── contexts/
│   └── PlaylistContext.tsx              ← NOVO
├── services/
│   ├── http.ts                          (já existe)
│   ├── musicas.service.ts              (já existe)
│   └── playlist.service.ts             ← NOVO
├── pages/Musicas/
│   ├── Musicas.tsx                      ← MODIFICADO
│   ├── types.ts                         ← MODIFICADO
│   ├── components/
│   │   ├── PainelPlaylist.tsx           ← NOVO
│   │   ├── ListaPlaylist.tsx            ← NOVO
│   │   ├── CriarPlaylistDialog.tsx      ← NOVO
│   │   ├── ListaMusicas.tsx             ← MODIFICADO
│   │   ├── PlayerMusica.tsx             ← MODIFICADO
│   │   ├── BuscadorMusicas.tsx          ← MODIFICADO
│   │   └── player/
│   │       ├── BlocoCentral.tsx         ← MODIFICADO
│   │       ├── BlocoDireita.tsx         ← MODIFICADO
│   │       └── MenuAdicionarPlaylist.tsx ← NOVO
│   └── hooks/
│       └── usePlayerAudio.ts            ← MODIFICADO
```

---

## Ordem de Implementação

| Fase | O que | Dependências |
|---|---|---|
| **1** | Pacote `Microsoft.Data.Sqlite` + schema music + FTS5 | Nenhuma |
| **2** | `MusicSearchService` + `MusicFileEntry` entity | Fase 1 |
| **3** | `MusicIndexerBackgroundService` (scan inicial + periódico) | Fase 2 |
| **4** | Endpoint `BuscarFullText` + integration | Fase 3 |
| **5** | Atualizar `BuscadorMusicas` no frontend | Fase 4 |
| **6** | Schema playlists + `PlaylistService` + controller | Fase 1 |
| **7** | `playlist.service.ts` + `PlaylistContext` no frontend | Fase 6 |
| **8** | Componentes de UI (PainelPlaylist, dialogs, etc.) | Fase 7 |
| **9** | Integração no Player (shuffle/repeat, adicionar à playlist) | Fase 8 |
| **10** | Drag-to-reorder com `@dnd-kit` | Fase 9 |

---

## Decisões de Design

- **SQLite** via `Microsoft.Data.Sqlite` — ADO.NET puro, sem EF Core
- **FTS5 híbrido** — `unicode61` para metadados (palavras inteiras) + `trigram` para filename/caminho (substring)
- **BM25 ranking** para resultados relevantes primeiro em metadados
- **Background scan** — não bloqueia inicialização da API
- **Playlists no backend** — persistem entre dispositivos
- **Músicas referenciadas por `relativePath`** — quebra se arquivo for movido/renomeado (aceitável)
- **Shuffle** — array embaralhado de índices, não modifica ordem original
- **Repeat** — `off` (para no fim), `all` (volta ao início), `one` (repete atual)
