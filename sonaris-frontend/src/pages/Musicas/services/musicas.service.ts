import { http } from '../../../services/http';
import type { BasePagedResponse, BaseResponse } from '../../../types/api';
import type { FileSystemItem, MusicMetadata } from '../types';

const BASE_URL = import.meta.env.VITE_API_URL as string;

export const getMusicas = (path: string, page: number, pageSize: number) =>
  http.get<BasePagedResponse<FileSystemItem>>('/api/Musica/BuscarMusicas', {
    params: { path, PageNumber: page, PageSize: pageSize },
  });

export const streamUrl = (relativePath: string) =>
  `${BASE_URL}/api/Musica/StreamArquivo?fileName=${encodeURIComponent(relativePath)}`;

export const getMusicaMetadata = (fileName: string) =>
  http.get<BaseResponse<MusicMetadata>>('/api/Musica/BuscarMusicaMetadata', {
    params: { fileName },
  });

export const getCapaMusica = (fileName: string) =>
  http.get(`/api/Musica/StreamCapa`, {
    params: { fileName },
    responseType: 'blob',
  });
