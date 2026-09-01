import { ListMusic, Plus } from 'lucide-react';
import type { Playlist } from '../types';

interface ISidebarPlaylists {
    playlists: Playlist[];
    playlistAtiva: Playlist | null;
    onSelecionar: (playlist: Playlist) => void;
    onCriar: () => void;
}

export function SidebarPlaylists({ playlists, playlistAtiva, onSelecionar, onCriar }: ISidebarPlaylists) {
    return (
        <div className="py-3">
            <div className="px-4 pb-2">
                <span className="text-xs font-semibold uppercase tracking-wide text-slate-400">Playlists</span>
            </div>
            <div className="px-3">
                <button
                    onClick={onCriar}
                    className="flex items-center gap-2 w-full px-3 py-2 rounded-lg text-sm font-medium text-blue-600 dark:text-blue-400 hover:bg-blue-50 dark:hover:bg-blue-900/20 transition-colors"
                >
                    <Plus size={15} />
                    Nova playlist
                </button>
            </div>
            {playlists.length > 0 && (
                <div className="mt-1 px-3 space-y-0.5">
                    {playlists.map((p) => (
                        <button
                            key={p.Id}
                            onClick={() => onSelecionar(p)}
                            className={`flex items-center gap-2 w-full px-3 py-2 rounded-lg text-sm transition-colors ${
                                playlistAtiva?.Id === p.Id
                                    ? 'bg-blue-50 dark:bg-blue-900/20 text-blue-700 dark:text-blue-300'
                                    : 'text-slate-700 dark:text-slate-300 hover:bg-slate-100 dark:hover:bg-slate-700/50'
                            }`}
                        >
                            <ListMusic size={15} className="shrink-0" />
                            <span className="truncate">{p.Name}</span>
                            {p.Tracks && p.Tracks.length > 0 && (
                                <span className="ml-auto text-xs text-slate-400 dark:text-slate-500">{p.Tracks.length}</span>
                            )}
                        </button>
                    ))}
                </div>
            )}
        </div>
    );
}