import { createContext, useCallback, useEffect, useRef, useState, type ReactNode } from 'react';
import type { Playlist } from '../pages/Musicas/types';
import * as playlistService from '../pages/Musicas/services/playlist.service';
import { erroParaMensagem } from '../services/httpError';
import { useToast } from './useToast';

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
    const playlistAtivaRef = useRef<Playlist | null>(null);
    const toast = useToast();

    useEffect(() => {
        playlistAtivaRef.current = playlistAtiva;
    }, [playlistAtiva]);

    const recarregar = useCallback(async () => {
        setLoading(true);
        try {
            const res = await playlistService.listarPlaylists();
            const data = res.data.Data;
            if (data) {
                setPlaylists(data);
                const atual = playlistAtivaRef.current;
                if (atual) {
                    const atualizada = data.find((p: Playlist) => p.Id === atual.Id) ?? null;
                    setPlaylistAtiva(atualizada);
                }
            }
        } catch (error) {
            toast.error(await erroParaMensagem(error));
        } finally {
            setLoading(false);
        }
    }, [toast]);

    useEffect(() => {
        recarregar();
    }, []); // eslint-disable-line react-hooks/exhaustive-deps

    const criar = async (name: string) => {
        try {
            const res = await playlistService.criarPlaylist(name);
            const nova = res.data.Data;
            if (!nova) throw new Error('Erro ao criar playlist.');
            await recarregar();
            toast.success('Playlist criada com sucesso.');
            return nova;
        } catch (error) {
            const mensagem = await erroParaMensagem(error);
            toast.error(mensagem);
            throw new Error(mensagem);
        }
    };

    const renomear = async (id: string, name: string) => {
        try {
            await playlistService.renomearPlaylist(id, name);
            await recarregar();
            toast.success('Playlist renomeada com sucesso.');
        } catch (error) {
            toast.error(await erroParaMensagem(error));
        }
    };

    const deletar = async (id: string) => {
        try {
            await playlistService.deletarPlaylist(id);
            if (playlistAtiva?.Id === id) setPlaylistAtiva(null);
            await recarregar();
            toast.success('Playlist deletada com sucesso.');
        } catch (error) {
            toast.error(await erroParaMensagem(error));
        }
    };

    const adicionarFaixa = async (playlistId: string, relativePath: string) => {
        try {
            await playlistService.adicionarFaixaPlaylist(playlistId, relativePath);
            await recarregar();
            toast.success('Música adicionada à playlist.');
        } catch (error) {
            toast.error(await erroParaMensagem(error));
        }
    };

    const removerFaixa = async (playlistId: string, trackId: number) => {
        try {
            await playlistService.removerFaixaPlaylist(playlistId, trackId);
            await recarregar();
            toast.success('Música removida da playlist.');
        } catch (error) {
            toast.error(await erroParaMensagem(error));
        }
    };

    const reordenarFaixa = async (playlistId: string, trackId: number, newPosition: number) => {
        try {
            await playlistService.reordenarFaixaPlaylist(playlistId, trackId, newPosition);
            await recarregar();
            toast.success('Faixa reordenada.');
        } catch (error) {
            toast.error(await erroParaMensagem(error));
        }
    };

    const duplicar = async (id: string, newName: string) => {
        try {
            await playlistService.duplicarPlaylist(id, newName);
            await recarregar();
            toast.success('Playlist duplicada com sucesso.');
        } catch (error) {
            toast.error(await erroParaMensagem(error));
        }
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
