import { http } from '../../../services/http';
import { erroParaMensagem, mensagemDeBlobErro } from '../../../services/httpError';

export interface DownloadTracksRequest {
    trackIds: number[];
}

export interface DownloadResponse {
    blob: Blob;
    fileName?: string;
}

function extrairFileName(contentDisposition: string | null): string | undefined {
    if (!contentDisposition) return undefined;
    const match = /filename\*=UTF-8''([^;]+)/i.exec(contentDisposition) ?? /filename="?([^";]+)"?/i.exec(contentDisposition);
    if (!match) return undefined;
    try {
        return decodeURIComponent(match[1]);
    } catch {
        return match[1];
    }
}

export async function downloadPlaylistTracks(
    playlistId: string,
    trackIds: number[],
): Promise<DownloadResponse> {
    try {
        const response = await http.post<Blob>(
            `/api/Playlist/${playlistId}/download`,
            { trackIds } as DownloadTracksRequest,
            { responseType: 'blob' },
        );

        if (response.data.type === 'application/json') {
            const mensagem = await mensagemDeBlobErro(response.data);
            throw new Error(mensagem ?? 'Não foi possível concluir o download.');
        }

        const contentDisposition = response.headers['content-disposition'] as string | undefined;

        return {
            blob: response.data,
            fileName: extrairFileName(contentDisposition ?? null),
        };
    } catch (error) {
        throw new Error(await erroParaMensagem(error));
    }
}

export function triggerDownload(blob: Blob, fileName?: string) {
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    if (fileName) {
        a.download = fileName;
    }
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
}