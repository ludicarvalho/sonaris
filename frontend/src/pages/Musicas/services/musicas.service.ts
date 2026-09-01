import { http } from '../../../services/http';
import { getToken } from '../../../services/auth.storage';
import type { BasePagedResponse, BaseResponse } from '../../../types/api';
import type { FileSystemItem, MusicMetadata } from '../types';

const BASE_URL = (import.meta.env.VITE_API_URL as string) ?? '';

export const getMusicas = (path: string, page: number, pageSize: number) =>
  http.get<BasePagedResponse<FileSystemItem>>('/api/Musica/BuscarMusicas', {
    params: { path, PageNumber: page, PageSize: pageSize },
  });

export const buscarMusicasPorNome = (termo: string, signal?: AbortSignal) =>
  http.get<BaseResponse<FileSystemItem[]>>('/api/Musica/BuscarPorNome', {
    params: { termo },
    signal,
  });

export const streamUrl = (relativePath: string) => {
  const token = getToken();
  const query = token ? `&token=${encodeURIComponent(token)}` : '';
  return `${BASE_URL}/api/Musica/StreamArquivo?fileName=${encodeURIComponent(relativePath)}${query}`;
};

export const getMusicaMetadata = (fileName: string) =>
  http.get<BaseResponse<MusicMetadata>>('/api/Musica/BuscarMusicaMetadata', {
    params: { fileName },
  });

export const getCapaMusica = (fileName: string) =>
  http.get(`/api/Musica/StreamCapa`, {
    params: { fileName },
    responseType: 'blob',
  });

export interface EditarMetadadosParams {
  fileName: string;
  title?: string;
  artist?: string;
  album?: string;
  track?: string;
  year?: string;
  removerCapa?: boolean;
  capa?: File | null;
}

export const editarMetadados = (params: EditarMetadadosParams) => {
  const formData = new FormData();
  formData.append('fileName', params.fileName);
  formData.append('title', params.title ?? '');
  formData.append('artist', params.artist ?? '');
  formData.append('album', params.album ?? '');
  formData.append('track', params.track ?? '');
  formData.append('year', params.year ?? '');
  formData.append('removerCapa', String(!!params.removerCapa));
  if (params.capa) formData.append('capa', params.capa);

  return http.post<BaseResponse<string>>('/api/Musica/EditarMetadados', formData);
};
