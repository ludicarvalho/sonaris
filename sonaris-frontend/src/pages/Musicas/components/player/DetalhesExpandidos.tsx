import { Capa } from "./Capa";
import { formatarDuracao, formatarTamanho } from "../../utils";
import type { FileSystemItem, MusicMetadata } from "../../types";

interface IDetalhesExpandidos {
    capaUrl: string | null;
    titulo: string;
    artistaAlbum: string;
    metadata: MusicMetadata | null;
    track: FileSystemItem;
}

export function DetalhesExpandidos({ capaUrl, titulo, artistaAlbum, metadata, track }: IDetalhesExpandidos) {
    return (
        <div className="border-b border-slate-200 dark:border-slate-700">
            <div className="max-w-4xl mx-auto px-4 py-6">
                <div className="flex flex-col sm:flex-row gap-6 sm:items-start">
                    <Capa capaUrl={capaUrl} grande />
                    <div className="min-w-0 flex-1">
                        <p className="text-xs text-blue-500 font-semibold uppercase tracking-wide">Agora tocando</p>
                        <h2 className="text-2xl font-bold mt-1 truncate">{titulo}</h2>
                        <p className="text-slate-400 dark:text-slate-500 truncate">{artistaAlbum}</p>

                        <dl className="grid grid-cols-2 sm:grid-cols-3 gap-3 mt-5 text-sm">
                            <div className="bg-slate-50 dark:bg-slate-900 rounded-lg p-3">
                                <dt className="text-xs text-slate-400">Álbum</dt>
                                <dd className="font-medium truncate" title={metadata?.Album ?? ''}>{metadata?.Album || '—'}</dd>
                            </div>
                            <div className="bg-slate-50 dark:bg-slate-900 rounded-lg p-3">
                                <dt className="text-xs text-slate-400">Faixa</dt>
                                <dd className="font-medium">{metadata?.Track || '—'}</dd>
                            </div>
                            <div className="bg-slate-50 dark:bg-slate-900 rounded-lg p-3">
                                <dt className="text-xs text-slate-400">Ano</dt>
                                <dd className="font-medium">{metadata?.Year || '—'}</dd>
                            </div>
                            <div className="bg-slate-50 dark:bg-slate-900 rounded-lg p-3">
                                <dt className="text-xs text-slate-400">Duração</dt>
                                <dd className="font-medium">{formatarDuracao(metadata?.Duration ?? null) || '—'}</dd>
                            </div>
                            <div className="bg-slate-50 dark:bg-slate-900 rounded-lg p-3">
                                <dt className="text-xs text-slate-400">Bitrate</dt>
                                <dd className="font-medium">{metadata?.Bitrate ? `${metadata.Bitrate} kbps` : '—'}</dd>
                            </div>
                            <div className="bg-slate-50 dark:bg-slate-900 rounded-lg p-3">
                                <dt className="text-xs text-slate-400">Tamanho</dt>
                                <dd className="font-medium">{formatarTamanho(track.Size)}</dd>
                            </div>
                        </dl>
                    </div>
                </div>
            </div>
        </div>
    );
}
