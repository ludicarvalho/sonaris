import { useState } from 'react';
import { Download, GripVertical, ListMusic, Music, Pencil, Trash2, X } from 'lucide-react';
import { usePlaylist } from '../../../hooks/usePlaylist';
import { useToast } from '../../../contexts/useToast';
import type { FileSystemItem, PlaylistTrack } from '../types';
import { arquivoDePath } from '../types';
import { removerExensaoArquivo } from '../../../utils/text';
import { downloadPlaylistTracks, triggerDownload } from '../services/download.service';
import { card } from '../styles';

interface IPainelPlaylist {
    currentTrack: FileSystemItem | null;
    onPlayTrack: (item: FileSystemItem) => void;
}

export function PainelPlaylist({ currentTrack, onPlayTrack }: IPainelPlaylist) {
    const { playlistAtiva, setPlaylistAtiva, removerFaixa, renomear, deletar, reordenarFaixa } = usePlaylist();
    const toast = useToast();
    const [editandoNome, setEditandoNome] = useState(false);
    const [novoNome, setNovoNome] = useState('');
    const [arrastandoId, setArrastandoId] = useState<number | null>(null);
    const [selecionados, setSelecionados] = useState<Set<number>>(new Set());
    const [baixando, setBaixando] = useState(false);

    if (!playlistAtiva) return null;

    const tracks: PlaylistTrack[] = playlistAtiva.Tracks;

    const toggleSelecionado = (trackId: number) => {
        setSelecionados((prev) => {
            const next = new Set(prev);
            if (next.has(trackId)) {
                next.delete(trackId);
            } else {
                next.add(trackId);
            }
            return next;
        });
    };

    const selecionarTodas = () => {
        if (selecionados.size === tracks.length && tracks.length > 0) {
            setSelecionados(new Set());
        } else {
            setSelecionados(new Set(tracks.map((t) => t.Id)));
        }
    };

    const handleBaixar = async () => {
        if (baixando) return;
        const selectedIds = Array.from(selecionados);
        if (selectedIds.length === 0 || !playlistAtiva) return;

        setBaixando(true);
        try {
            const { blob, fileName } = await downloadPlaylistTracks(playlistAtiva.Id, selectedIds);

            if (selectedIds.length === 1) {
                triggerDownload(blob, fileName);
            } else {
                triggerDownload(blob, `${playlistAtiva.Name}.zip`);
            }

            toast.success('Download iniciado.');
            setSelecionados(new Set());
        } catch (error) {
            const mensagem = error instanceof Error ? error.message : 'Não foi possível concluir o download.';
            console.error('Erro ao baixar:', error);
            toast.error(mensagem);
        } finally {
            setBaixando(false);
        }
    };

    const iniciarEdicao = () => {
        setNovoNome(playlistAtiva.Name);
        setEditandoNome(true);
    };

    const salvarEdicao = async () => {
        const nome = novoNome.trim();
        if (nome && nome !== playlistAtiva.Name) {
            await renomear(playlistAtiva.Id, nome);
        }
        setEditandoNome(false);
    };

    const handleDeletar = async () => {
        if (confirm(`Deletar a playlist "${playlistAtiva.Name}"?`)) {
            await deletar(playlistAtiva.Id);
        }
    };

    const aoPlayTrack = (relativePath: string) => {
        onPlayTrack(arquivoDePath(relativePath));
    };

    const handleDrop = async (targetId: number) => {
        if (arrastandoId === null || arrastandoId === targetId) return;
        const de = tracks.findIndex((t) => t.Id === arrastandoId);
        const para = tracks.findIndex((t) => t.Id === targetId);
        if (de === -1 || para === -1) return;
        await reordenarFaixa(playlistAtiva.Id, arrastandoId, para);
    };

    return (
        <div className={card}>
            <div className="flex items-center gap-3 px-4 py-3 border-b border-slate-100 dark:border-slate-700">
                <ListMusic size={18} className="shrink-0 text-blue-500" />
                <div className="min-w-0 flex-1">
                    {editandoNome ? (
                        <input
                            autoFocus
                            value={novoNome}
                            onChange={(e) => setNovoNome(e.target.value)}
                            onBlur={salvarEdicao}
                            onKeyDown={(e) => {
                                if (e.key === 'Enter') salvarEdicao();
                                if (e.key === 'Escape') setEditandoNome(false);
                            }}
                            className="w-full text-sm font-semibold bg-transparent border-b border-blue-500 text-slate-900 dark:text-white outline-none pb-0.5"
                        />
                    ) : (
                        <span
                            className="text-sm font-semibold text-slate-900 dark:text-white truncate block cursor-pointer hover:text-blue-600 dark:hover:text-blue-400 transition-colors"
                            onClick={iniciarEdicao}
                            title="Clique para renomear"
                        >
                            {playlistAtiva.Name}
                        </span>
                    )}
                    <span className="text-xs text-slate-400 dark:text-slate-500">
                        {tracks.length} {tracks.length === 1 ? 'faixa' : 'faixas'}
                    </span>
                </div>
                <div className="flex items-center gap-1 shrink-0">
                    <button
                        onClick={iniciarEdicao}
                        title="Renomear"
                        className="inline-flex items-center justify-center w-7 h-7 rounded-lg text-slate-400 hover:text-slate-800 dark:hover:text-white hover:bg-slate-100 dark:hover:bg-slate-700 transition-colors"
                    >
                        <Pencil size={14} />
                    </button>
                    <button
                        onClick={handleDeletar}
                        title="Deletar playlist"
                        className="inline-flex items-center justify-center w-7 h-7 rounded-lg text-slate-400 hover:text-red-500 hover:bg-red-50 dark:hover:bg-red-900/20 transition-colors"
                    >
                        <Trash2 size={14} />
                    </button>
                    <button
                        onClick={() => setPlaylistAtiva(null)}
                        title="Fechar playlist"
                        className="inline-flex items-center justify-center w-7 h-7 rounded-lg text-slate-400 hover:text-slate-800 dark:hover:text-white hover:bg-slate-100 dark:hover:bg-slate-700 transition-colors"
                    >
                        <X size={14} />
                    </button>
                </div>
            </div>

            <div className="max-h-80 overflow-y-auto">
                {tracks.length === 0 ? (
                    <div className="px-4 py-8 text-center">
                        <Music size={24} className="mx-auto mb-2 text-slate-300 dark:text-slate-600" />
                        <p className="text-sm text-slate-400 dark:text-slate-500">Playlist vazia</p>
                        <p className="text-xs text-slate-400 dark:text-slate-500 mt-1">Clique em "+" ao lado de uma música para adicionar</p>
                    </div>
                ) : (
                    <div className="divide-y divide-slate-50 dark:divide-slate-700/50">
                        {tracks.map((track) => {
                            const isPlaying = currentTrack?.RelativePath === track.RelativePath;
                            return (
                                <div
                                    key={track.Id}
                                    draggable
                                    onDragStart={() => setArrastandoId(track.Id)}
                                    onDragEnd={() => setArrastandoId(null)}
                                    onDragOver={(e) => {
                                        e.preventDefault();
                                        e.dataTransfer.dropEffect = 'move';
                                    }}
                                    onDrop={(e) => {
                                        e.preventDefault();
                                        handleDrop(track.Id);
                                    }}
                                    className={`flex items-center gap-2 px-3 py-2 group transition-colors ${arrastandoId === track.Id ? 'opacity-50' : ''} ${isPlaying ? 'bg-blue-50 dark:bg-blue-900/20' : arrastandoId !== null ? 'cursor-grab hover:bg-slate-100 dark:hover:bg-slate-700/60' : 'hover:bg-slate-50 dark:hover:bg-slate-700/30'}`}
                                >
                                <label
                                    className="shrink-0 inline-flex items-center cursor-pointer"
                                    title="Selecionar para download"
                                    onClick={(e) => e.stopPropagation()}
                                >
                                    <input
                                        type="checkbox"
                                        checked={selecionados.has(track.Id)}
                                        onChange={() => toggleSelecionado(track.Id)}
                                        className="w-4 h-4 rounded border-slate-300 text-blue-600 focus:ring-blue-500"
                                    />
                                </label>
                                <span
                                    draggable
                                    onDragStart={(e) => {
                                        e.stopPropagation();
                                        setArrastandoId(track.Id);
                                    }}
                                    title="Arraste para reordenar"
                                    className="shrink-0 inline-flex cursor-grab active:cursor-grabbing"
                                >
                                    <GripVertical size={14} className="text-slate-300 dark:text-slate-600 group-hover:text-slate-400 dark:group-hover:text-slate-500" />
                                </span>
                                <button
                                    onClick={() => aoPlayTrack(track.RelativePath)}
                                    className="flex-1 min-w-0 text-left cursor-pointer"
                                >
                                    <span className={`block text-sm truncate transition-colors ${isPlaying ? 'text-blue-600 dark:text-blue-400 font-medium' : 'text-slate-700 dark:text-slate-300 hover:text-blue-600 dark:hover:text-blue-400'}`}>
                                        {track.Title || removerExensaoArquivo(track.RelativePath.split('/').pop() ?? '')}
                                    </span>
                                    {(track.Artist || track.Album) && (
                                        <span className="block text-xs text-slate-400 dark:text-slate-500 truncate">
                                            {[track.Artist, track.Album].filter(Boolean).join(' • ')}
                                        </span>
                                    )}
                                </button>
                                <button
                                    onClick={() => removerFaixa(playlistAtiva.Id, track.Id)}
                                    title="Remover da playlist"
                                    className="inline-flex items-center justify-center w-6 h-6 rounded text-slate-300 dark:text-slate-600 hover:text-red-500 opacity-0 group-hover:opacity-100 transition-all"
                                >
                                    <X size={13} />
                                </button>
                            </div>
                        );
                        })}
                    </div>
                )}
            </div>

            {tracks.length > 0 && (
                <div className="flex items-center justify-between gap-2 px-4 py-2.5 border-t border-slate-100 dark:border-slate-700">
                    <div className="flex items-center gap-2 min-w-0">
                        <button
                            onClick={selecionarTodas}
                            className="px-3 py-1.5 text-xs font-medium text-slate-600 dark:text-slate-400 hover:text-slate-800 dark:hover:text-white hover:bg-slate-100 dark:hover:bg-slate-700 rounded-lg transition-colors"
                        >
                            {selecionados.size === tracks.length && tracks.length > 0 ? 'Desmarcar todas' : 'Selecionar todas'}
                        </button>
                        {selecionados.size > 0 && (
                            <span className="text-xs text-slate-500 dark:text-slate-400 shrink-0">
                                {selecionados.size} selecionada{selecionados.size > 1 ? 's' : ''}
                            </span>
                        )}
                    </div>
                    <button
                        onClick={handleBaixar}
                        disabled={selecionados.size === 0 || baixando}
                        title="Baixar músicas selecionadas"
                        className="inline-flex items-center gap-2 px-4 py-2 text-sm font-medium text-white bg-blue-600 hover:bg-blue-700 rounded-lg transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
                    >
                        <Download size={16} />
                        {baixando ? 'Baixando...' : `Baixar (${selecionados.size})`}
                    </button>
                </div>
            )}
        </div>
    );
}
