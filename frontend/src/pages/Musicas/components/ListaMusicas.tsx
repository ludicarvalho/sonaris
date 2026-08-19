import { useState } from 'react';
import { AudioLines, Folder, FolderUp, ListPlus, Loader2, Music2, Search } from "lucide-react";
import type { RefObject } from "react";
import type { FileSystemItem } from "../types";
import { formatarData, formatarTamanho } from "../utils";
import { AdicionarPlaylistMenu } from "./AdicionarPlaylistMenu";
import { CriarPlaylistDialog } from "./CriarPlaylistDialog";
import { usePlaylist } from "../../../hooks/usePlaylist";

interface IListaMusicas {
    items: FileSystemItem[];
    loading: boolean;
    loadingMore: boolean;
    hasMore: boolean;
    totalItems: number;
    isRoot: boolean;
    currentTrack: FileSystemItem | null;
    sentinelRef: RefObject<HTMLDivElement | null>;
    onSelect: (item: FileSystemItem) => void;
    onUp: () => void;
}

export function ListaMusicas({
    items,
    loading,
    loadingMore,
    hasMore,
    totalItems,
    isRoot,
    currentTrack,
    sentinelRef,
    onSelect,
    onUp,
}: IListaMusicas) {
    const { criar } = usePlaylist();
    const [menuTrackPath, setMenuTrackPath] = useState<string | null>(null);
    const [dialogCriarAberto, setDialogCriarAberto] = useState(false);

    return (
        <div className="bg-white dark:bg-slate-800 rounded-xl border border-slate-200 dark:border-slate-700 shadow-sm overflow-hidden">
            <div className="overflow-x-auto">
                <table className="w-full text-sm">
                    <thead className="bg-slate-50 dark:bg-slate-900 border-b border-slate-200 dark:border-slate-700">
                        <tr>
                            <th className="px-4 py-3 text-left text-xs font-semibold text-slate-500 dark:text-slate-400 uppercase tracking-wide">Nome</th>
                            <th className="px-4 py-3 text-left text-xs font-semibold text-slate-500 dark:text-slate-400 uppercase tracking-wide">Tamanho</th>
                            <th className="px-4 py-3 text-left text-xs font-semibold text-slate-500 dark:text-slate-400 uppercase tracking-wide">Modificado em</th>
                            <th className="w-10"></th>
                        </tr>
                    </thead>
                    <tbody className="divide-y divide-slate-100 dark:divide-slate-700">
                        {!isRoot && (
                            <tr className="hover:bg-slate-50 dark:hover:bg-slate-700/50 transition-colors cursor-pointer" onClick={onUp}>
                                <td colSpan={4} className="px-4 py-3">
                                    <span className="inline-flex items-center gap-2 text-slate-500 dark:text-slate-400 font-medium">
                                        <FolderUp size={18} className="shrink-0" />
                                        Voltar
                                    </span>
                                </td>
                            </tr>
                        )}

                        {loading ? (
                            Array.from({ length: 6 }).map((_, i) => (
                                <tr key={i}>
                                    <td colSpan={4} className="px-4 py-3">
                                        <div className="h-4 bg-slate-100 dark:bg-slate-700 rounded animate-pulse" />
                                    </td>
                                </tr>
                            ))
                        ) : items.length === 0 ? (
                            <tr>
                                <td colSpan={4} className="px-4 py-12 text-center text-slate-400">
                                    <Search size={32} className="mx-auto mb-2 opacity-30" />
                                    Nenhum arquivo ou pasta encontrado
                                </td>
                            </tr>
                        ) : items.map(item => {
                            const isPlaying = !item.IsDirectory && currentTrack?.RelativePath === item.RelativePath;
                            const nomeExibicao = item.IsDirectory ? item.Name : item.Name.replace(/\.[^/.]+$/, '');

                            return (
                                <tr
                                    key={item.RelativePath}
                                    onClick={() => onSelect(item)}
                                    className={`cursor-pointer transition-colors ${isPlaying
                                        ? "bg-blue-50 dark:bg-blue-900/30"
                                        : "hover:bg-slate-50 dark:hover:bg-slate-700/50"
                                        }`}
                                >
                                    <td className="px-4 py-3">
                                        <span className="inline-flex items-center gap-2.5">
                                            {item.IsDirectory
                                                ? <Folder size={18} className="shrink-0 text-amber-500" />
                                                : isPlaying
                                                    ? <AudioLines size={18} className="shrink-0 text-blue-500" />
                                                    : <Music2 size={18} className="shrink-0 text-blue-500" />}
                                            <span className={`font-medium truncate ${isPlaying
                                                ? "text-blue-700 dark:text-blue-300"
                                                : "text-slate-700 dark:text-slate-300"
                                                }`}>
                                                {nomeExibicao}
                                            </span>
                                        </span>
                                    </td>
                                    <td className="px-4 py-3 text-slate-500 dark:text-slate-400">
                                        {item.IsDirectory ? '—' : formatarTamanho(item.Size)}
                                    </td>
                                    <td className="px-4 py-3 text-slate-500 dark:text-slate-400 font-mono">
                                        {formatarData(item.LastModified)}
                                    </td>
                                    <td className="px-2 py-3">
                                        {!item.IsDirectory && (
                                            <div className="relative">
                                                <button
                                                    onClick={(e) => {
                                                        e.stopPropagation();
                                                        setMenuTrackPath(menuTrackPath === item.RelativePath ? null : item.RelativePath);
                                                    }}
                                                    title="Adicionar à playlist"
                                                    className="inline-flex items-center justify-center w-7 h-7 rounded-md text-slate-400 dark:text-slate-500 hover:text-blue-500 hover:bg-blue-50 dark:hover:bg-blue-900/20 transition-colors opacity-0 group-hover:opacity-100"
                                                    style={{ opacity: menuTrackPath === item.RelativePath ? 1 : undefined }}
                                                >
                                                    <ListPlus size={15} />
                                                </button>
                                                <AdicionarPlaylistMenu
                                                    relativePath={item.RelativePath}
                                                    aberto={menuTrackPath === item.RelativePath}
                                                    onFechar={() => setMenuTrackPath(null)}
                                                    onCriarPlaylist={() => {
                                                        setMenuTrackPath(null);
                                                        setDialogCriarAberto(true);
                                                    }}
                                                />
                                            </div>
                                        )}
                                    </td>
                                </tr>
                            );
                        })}
                    </tbody>
                </table>
            </div>

            {!loading && (
                <div className="border-t border-slate-100 dark:border-slate-700">
                    <div ref={sentinelRef} className="flex items-center justify-center px-4 py-3">
                        {loadingMore ? (
                            <Loader2 size={20} className="animate-spin text-blue-500" />
                        ) : !hasMore ? (
                            <span className="text-xs text-slate-400">
                                {items.length} de {totalItems} itens
                            </span>
                        ) : (
                            <span className="text-xs text-slate-400">
                                {items.length} de {totalItems} itens — role para carregar mais
                            </span>
                        )}
                    </div>
                </div>
            )}

            <CriarPlaylistDialog
                aberto={dialogCriarAberto}
                onFechar={() => setDialogCriarAberto(false)}
                onCriar={async (nome) => {
                    await criar(nome);
                }}
            />
        </div>
    );
}
