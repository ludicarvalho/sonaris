import { useRef } from 'react';
import { ListPlus, Plus } from 'lucide-react';
import { usePlaylist } from '../../../hooks/usePlaylist';
import { useClickOutside } from '../../../hooks/useClickOutside';
import { dropdown } from '../styles';

interface IAdicionarPlaylistMenu {
    relativePath: string;
    aberto: boolean;
    onFechar: () => void;
    onCriarPlaylist: () => void;
}

export function AdicionarPlaylistMenu({ relativePath, aberto, onFechar, onCriarPlaylist }: IAdicionarPlaylistMenu) {
    const { playlists, adicionarFaixa } = usePlaylist();
    const menuRef = useRef<HTMLDivElement | null>(null);

    useClickOutside(menuRef, () => {
        if (aberto) onFechar();
    });

    if (!aberto) return null;

    return (
        <div ref={menuRef} className={dropdown}>
            <div className="px-3 py-2 border-b border-slate-100 dark:border-slate-700">
                <span className="text-xs font-semibold text-slate-500 dark:text-slate-400 uppercase tracking-wide">Adicionar à playlist</span>
            </div>

            <div className="max-h-48 overflow-y-auto py-1">
                {playlists.map((playlist) => (
                    <button
                        key={playlist.Id}
                        onClick={async () => {
                            await adicionarFaixa(playlist.Id, relativePath);
                            onFechar();
                        }}
                        className="w-full flex items-center gap-2 px-3 py-2 text-sm text-slate-700 dark:text-slate-300 hover:bg-slate-50 dark:hover:bg-slate-700/50 transition-colors text-left"
                    >
                        <ListPlus size={15} className="shrink-0 text-slate-400 dark:text-slate-500" />
                        <span className="truncate">{playlist.Name}</span>
                        <span className="ml-auto text-xs text-slate-400 dark:text-slate-500">{playlist.Tracks.length}</span>
                    </button>
                ))}

                {playlists.length === 0 && (
                    <div className="px-3 py-2 text-sm text-slate-400 dark:text-slate-500 italic">
                        Nenhuma playlist criada
                    </div>
                )}
            </div>

            <div className="border-t border-slate-100 dark:border-slate-700">
                <button
                    onClick={() => {
                        onCriarPlaylist();
                        onFechar();
                    }}
                    className="w-full flex items-center gap-2 px-3 py-2 text-sm font-medium text-blue-600 dark:text-blue-400 hover:bg-slate-50 dark:hover:bg-slate-700/50 transition-colors"
                >
                    <Plus size={15} className="shrink-0" />
                    Nova playlist
                </button>
            </div>
        </div>
    );
}
