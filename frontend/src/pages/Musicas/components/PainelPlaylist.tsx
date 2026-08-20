import { useState } from 'react';
import { GripVertical, ListMusic, Music, Pencil, Trash2, X } from 'lucide-react';
import { usePlaylist } from '../../../hooks/usePlaylist';
import type { FileSystemItem } from '../types';
import { removerExensaoArquivo } from '../../../utils/text';

interface IPainelPlaylist {
    currentTrack: import('../types').FileSystemItem | null;
    onPlayTrack: (item: FileSystemItem) => void;
}

export function PainelPlaylist({ currentTrack, onPlayTrack }: IPainelPlaylist) {
    const { playlistAtiva, setPlaylistAtiva, removerFaixa, renomear, deletar } = usePlaylist();
    const [editandoNome, setEditandoNome] = useState(false);
    const [novoNome, setNovoNome] = useState('');

    if (!playlistAtiva) return null;

    const tracks: import('../types').PlaylistTrack[] = playlistAtiva.Tracks;

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
        const fakeItem: FileSystemItem = {
            Name: relativePath.split('/').pop() ?? '',
            RelativePath: relativePath,
            IsDirectory: false,
            Size: null,
            LastModified: '',
        };
        onPlayTrack(fakeItem);
    };

    return (
        <div className="bg-white dark:bg-slate-800 rounded-xl border border-slate-200 dark:border-slate-700 shadow-sm overflow-hidden">
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
                                    className={`flex items-center gap-2 px-3 py-2 group transition-colors ${isPlaying ? 'bg-blue-50 dark:bg-blue-900/20' : 'hover:bg-slate-50 dark:hover:bg-slate-700/30'}`}
                                >
                                <GripVertical size={14} className="shrink-0 text-slate-300 dark:text-slate-600 cursor-grab" />
                                <button
                                    onClick={() => aoPlayTrack(track.RelativePath)}
                                    className="flex-1 min-w-0 text-left"
                                >
                                    <span className={`block text-sm truncate transition-colors ${isPlaying ? 'text-blue-600 dark:text-blue-400 font-medium' : 'text-slate-700 dark:text-slate-300 hover:text-blue-600 dark:hover:text-blue-400'}`}>
                                        {track.Title || removerExensaoArquivo(track.RelativePath.split('/').pop() ?? '')}
                                    </span>
                                    {track.Artist && (
                                        <span className="block text-xs text-slate-400 dark:text-slate-500 truncate">
                                            {track.Artist}
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
        </div>
    );
}
