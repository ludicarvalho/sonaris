import { createContext, useCallback, useEffect, useState, type ReactNode } from 'react';
import type { Playlist } from '../pages/Musicas/types';
import * as playlistService from '../pages/Musicas/services/playlist.service';

export interface PlaylistContextType {
    playlists: Playlist[];
    loading: boolean;
    playlistAtiva: Playlist | null;
    setPlaylistAtiva: (playlist: Playlist | null) => void;
    criar: (name: string) => Promise<Playlist>;
    renomear: (id: string, name: string) => Promise<void>;
    deletar: (id: string) => Promise<void>;
    adicionarFaixa: (playlistId: string, relativePath: string) => Promise<void>;
    removerFaixa: (playlistId: string, trackId: number) => Promise<void>;
    reordenarFaixa: (playlistId: string, trackId: number, newPosition: number) => Promise<void>;
    duplicar: (id: string, newName: string) => Promise<void>;
    recarregar: () => Promise<void>;
}

export const PlaylistContext = createContext<PlaylistContextType | null>(null);

export function PlaylistProvider({ children }: { children: ReactNode }) {
    const [playlists, setPlaylists] = useState<Playlist[]>([]);
    const [loading, setLoading] = useState(true);
    const [playlistAtiva, setPlaylistAtiva] = useState<Playlist | null>(null);

    const recarregar = useCallback(async () => {
        setLoading(true);
        try {
            const res = await playlistService.listarPlaylists();
            const data = res.data.Data;
            if (data) {
                setPlaylists(data);
                if (playlistAtiva) {
                    const atualizada = data.find((p: Playlist) => p.Id === playlistAtiva.Id) ?? null;
                    setPlaylistAtiva(atualizada);
                }
            }
        } finally {
            setLoading(false);
        }
    }, [playlistAtiva]);

    useEffect(() => {
        recarregar();
    }, []); // eslint-disable-line react-hooks/exhaustive-deps

    const criar = async (name: string) => {
        const res = await playlistService.criarPlaylist(name);
        const nova = res.data.Data;
        if (!nova) throw new Error('Erro ao criar playlist');
        await recarregar();
        return nova;
    };

    const renomear = async (id: string, name: string) => {
        await playlistService.renomearPlaylist(id, name);
        await recarregar();
    };

    const deletar = async (id: string) => {
        await playlistService.deletarPlaylist(id);
        if (playlistAtiva?.Id === id) setPlaylistAtiva(null);
        await recarregar();
    };

    const adicionarFaixa = async (playlistId: string, relativePath: string) => {
        await playlistService.adicionarFaixaPlaylist(playlistId, relativePath);
        await recarregar();
    };

    const removerFaixa = async (playlistId: string, trackId: number) => {
        await playlistService.removerFaixaPlaylist(playlistId, trackId);
        await recarregar();
    };

    const reordenarFaixa = async (playlistId: string, trackId: number, newPosition: number) => {
        await playlistService.reordenarFaixaPlaylist(playlistId, trackId, newPosition);
        await recarregar();
    };

    const duplicar = async (id: string, newName: string) => {
        await playlistService.duplicarPlaylist(id, newName);
        await recarregar();
    };

    return (
        <PlaylistContext.Provider value={{
            playlists,
            loading,
            playlistAtiva,
            setPlaylistAtiva,
            criar,
            renomear,
            deletar,
            adicionarFaixa,
            removerFaixa,
            reordenarFaixa,
            duplicar,
            recarregar,
        }}>
            {children}
        </PlaylistContext.Provider>
    );
}
