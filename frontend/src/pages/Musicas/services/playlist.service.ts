import { http } from '../../../services/http';
import type { BaseResponse } from '../../../types/api';
import type { Playlist, PlaylistTrack, MusicSearchResult } from '../types';

export const listarPlaylists = () =>
    http.get<BaseResponse<Playlist[]>>('/api/Playlist');

export const obterPlaylist = (id: string) =>
    http.get<BaseResponse<Playlist>>(`/api/Playlist/${id}`);

export const criarPlaylist = (name: string) =>
    http.post<BaseResponse<Playlist>>('/api/Playlist', JSON.stringify(name), {
        headers: { 'Content-Type': 'application/json' },
    });

export const renomearPlaylist = (id: string, name: string) =>
    http.put<BaseResponse<Playlist>>(`/api/Playlist/${id}`, JSON.stringify(name), {
        headers: { 'Content-Type': 'application/json' },
    });

export const deletarPlaylist = (id: string) =>
    http.delete<BaseResponse<object>>(`/api/Playlist/${id}`);

export const adicionarFaixaPlaylist = (playlistId: string, relativePath: string) =>
    http.post<BaseResponse<PlaylistTrack>>(
        `/api/Playlist/${playlistId}/tracks`,
        null,
        { params: { relativePath } },
    );

export const removerFaixaPlaylist = (playlistId: string, trackId: number) =>
    http.delete<BaseResponse<object>>(`/api/Playlist/${playlistId}/tracks/${trackId}`);

export const reordenarFaixaPlaylist = (playlistId: string, trackId: number, newPosition: number) =>
    http.put<BaseResponse<object>>(
        `/api/Playlist/${playlistId}/tracks/${trackId}/reorder`,
        null,
        { params: { newPosition } },
    );

export const duplicarPlaylist = (id: string, newName: string) =>
    http.post<BaseResponse<Playlist>>(`/api/Playlist/${id}/duplicate`, null, {
        params: { newName },
    });

export const buscarFullText = (termo: string, pageNumber: number = 1, pageSize: number = 30) =>
    http.get<{ Data: MusicSearchResult[]; Success: boolean; Message: string; Pages: number; ItemsTotal: number; PageInfo: { PageNumber: number; PageSize: number } }>(
        '/api/Musica/BuscarFullText',
        { params: { termo, pageNumber, pageSize } },
    );
