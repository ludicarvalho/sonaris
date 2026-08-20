import { useCallback, useEffect, useRef, useState } from 'react';
import { useSearchParams } from 'react-router-dom';
import { AlertCircle, ListMusic, Moon, Music4, Plus, Sun } from 'lucide-react';
import { getMusicas } from './services/musicas.service';
import type { FileSystemItem } from './types';
import { BreadcrumbMusicas } from './components/BreadcrumbMusicas';
import { BuscadorMusicas } from './components/BuscadorMusicas';
import { ListaMusicas } from './components/ListaMusicas';
import { PainelPlaylist } from './components/PainelPlaylist';
import { PlayerMusica } from './components/PlayerMusica';
import { CriarPlaylistDialog } from './components/CriarPlaylistDialog';
import { PlaylistProvider } from '../../contexts/PlaylistContext';
import { usePageTitle } from '../../hooks/usePageTitle';
import { useTheme } from '../../contexts/useTheme';
import { usePlaylist } from '../../hooks/usePlaylist';
import { removerExensaoArquivo } from '../../utils/text';

const PAGE_SIZE = 30;

function MusicasInner() {
    const { theme, toggleTheme } = useTheme();
    const { playlists, playlistAtiva, setPlaylistAtiva, criar } = usePlaylist();
    const [searchParams, setSearchParams] = useSearchParams();
    const path = searchParams.get('path') ?? '';
    const [items, setItems] = useState<FileSystemItem[]>([]);
    const [loading, setLoading] = useState(true);
    const [loadingMore, setLoadingMore] = useState(false);
    const [error, setError] = useState('');
    const [currentTrack, setCurrentTrack] = useState<FileSystemItem | null>(null);
    const [page, setPage] = useState(1);
    const [totalPages, setTotalPages] = useState(1);
    const [totalItems, setTotalItems] = useState(0);
    const [dialogCriarAberto, setDialogCriarAberto] = useState(false);
    const [menuPlaylistsAberto, setMenuPlaylistsAberto] = useState(false);
    const menuRef = useRef<HTMLDivElement | null>(null);
    const sentinelRef = useRef<HTMLDivElement | null>(null);

    const tituloFaixa = currentTrack ? removerExensaoArquivo(currentTrack.Name) : undefined;
    usePageTitle(tituloFaixa);

    const navigateTo = (p: string) => {
        setSearchParams(p ? { path: p } : {}, { replace: false });
    };

    useEffect(() => {
        let active = true;
        setLoading(true);
        setError('');
        setItems([]);
        setPage(1);
        setTotalPages(1);
        setTotalItems(0);

        getMusicas(path, 1, PAGE_SIZE)
            .then((res) => {
                if (!active) return;
                const { Data, Success, Message, Pages, ItemsTotal } = res.data;
                if (Success) {
                    setItems(Data ?? []);
                    setTotalPages(Pages);
                    setTotalItems(ItemsTotal);
                } else {
                    setError(Message ?? 'Não foi possível carregar as músicas.');
                }
            })
            .catch((err: any) => {
                if (!active) return;
                setError(err?.response?.data?.Message ?? err?.message ?? 'Erro ao conectar com o servidor.');
            })
            .finally(() => {
                if (active) setLoading(false);
            });

        return () => {
            active = false;
        };
    }, [path]);

    const loadMoreRef = useRef<() => void>(() => { });

    const loadMore = useCallback(() => {
        if (loading || loadingMore) return;
        if (page >= totalPages) return;

        setLoadingMore(true);
        getMusicas(path, page + 1, PAGE_SIZE)
            .then((res) => {
                const { Data, Success, Pages, ItemsTotal } = res.data;
                if (Success) {
                    setItems(prev => [...prev, ...(Data ?? [])]);
                    setTotalPages(Pages);
                    setTotalItems(ItemsTotal);
                    setPage(p => p + 1);
                }
            })
            .catch(() => { })
            .finally(() => {
                setLoadingMore(false);
                requestAnimationFrame(() => {
                    const el = sentinelRef.current;
                    if (el && el.getBoundingClientRect().top <= window.innerHeight) {
                        loadMoreRef.current();
                    }
                });
            });
    }, [loading, loadingMore, page, totalPages, path]);

    loadMoreRef.current = loadMore;

    useEffect(() => {
        const el = sentinelRef.current;
        if (!el || loading) return;

        const observer = new IntersectionObserver(
            (entries) => {
                if (entries[0].isIntersecting) loadMoreRef.current();
            },
            { rootMargin: '200px' },
        );
        observer.observe(el);

        return () => observer.disconnect();
    }, [loading, path]);

    const handleSelect = (item: FileSystemItem) => {
        if (item.IsDirectory) {
            navigateTo(item.RelativePath);
        } else {
            setCurrentTrack(item);
        }
    };

    const handleSelectBusca = (relativePath: string) => {
        const idx = relativePath.lastIndexOf('/');
        const pasta = idx > 0 ? relativePath.slice(0, idx) : '';
        navigateTo(pasta);

        const fakeItem: FileSystemItem = {
            Name: relativePath.split('/').pop() ?? '',
            RelativePath: relativePath,
            IsDirectory: false,
            Size: null,
            LastModified: '',
        };
        setCurrentTrack(fakeItem);
    };

    const faixas = items.filter(item => !item.IsDirectory);
    const faixaAtualIdx = faixas.findIndex(f => f.RelativePath === currentTrack?.RelativePath);

    const irParaFaixa = (delta: number) => {
        const idx = faixas.findIndex(f => f.RelativePath === currentTrack?.RelativePath);
        const proxima = idx + delta;
        if (proxima >= 0 && proxima < faixas.length) setCurrentTrack(faixas[proxima]);
    };

    const handleUp = () => {
        navigateTo(path.split('/').slice(0, -1).join('/'));
    };

    useEffect(() => {
        const handleClickOutside = (e: MouseEvent) => {
            if (menuRef.current && !menuRef.current.contains(e.target as Node)) {
                setMenuPlaylistsAberto(false);
            }
        };
        if (menuPlaylistsAberto) {
            document.addEventListener('mousedown', handleClickOutside);
            return () => document.removeEventListener('mousedown', handleClickOutside);
        }
    }, [menuPlaylistsAberto]);

    return (
        <div className="min-h-screen bg-gradient-to-br from-slate-100 via-slate-50 to-blue-50 text-slate-900 dark:from-slate-900 dark:via-slate-900 dark:to-blue-950 dark:text-white">
            <div className={`max-w-4xl mx-auto px-4 py-8 ${currentTrack ? 'pb-44' : 'pb-12'}`}>
                <header className="flex items-start justify-between gap-4 mb-6">
                    <div className="flex items-center gap-4">
                        <div className="flex items-center justify-center w-12 h-12 rounded-2xl bg-gradient-to-br from-blue-500 to-indigo-600 shadow-lg shadow-blue-600/40 shrink-0">
                            <Music4 size={24} className="text-white" />
                        </div>
                        <div>
                            <h1 className="text-2xl font-bold">Músicas</h1>
                            <p className="text-slate-500 dark:text-slate-400 text-sm">Navegue pelas pastas e clique em uma faixa para tocar</p>
                        </div>
                    </div>
                    <div className="flex items-center gap-2 shrink-0">
                        <div className="relative" ref={menuRef}>
                            <button
                                onClick={() => setMenuPlaylistsAberto(!menuPlaylistsAberto)}
                                title="Playlists"
                                className="inline-flex items-center gap-1.5 px-3 py-2 text-sm font-medium text-slate-600 dark:text-slate-400 hover:text-blue-600 dark:hover:text-blue-400 bg-slate-200/70 dark:bg-slate-800/60 hover:bg-blue-50 dark:hover:bg-blue-900/20 rounded-lg transition-colors"
                            >
                                <ListMusic size={16} />
                                <span className="hidden sm:inline">Playlists</span>
                            </button>
                            {menuPlaylistsAberto && (
                                <div className="absolute right-0 top-full mt-1.5 z-30 bg-white dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-xl shadow-lg overflow-hidden min-w-[220px]">
                                    <button
                                        onClick={() => { setMenuPlaylistsAberto(false); setDialogCriarAberto(true); }}
                                        className="flex items-center gap-2 w-full px-4 py-2.5 text-sm text-blue-600 dark:text-blue-400 hover:bg-slate-50 dark:hover:bg-slate-700/50 transition-colors"
                                    >
                                        <Plus size={15} />
                                        Criar playlist
                                    </button>
                                    {playlists.length > 0 && (
                                        <>
                                            <div className="border-t border-slate-100 dark:border-slate-700" />
                                            <div className="max-h-64 overflow-y-auto py-1">
                                                {playlists.map((p) => (
                                                    <button
                                                        key={p.Id}
                                                        onClick={() => { setPlaylistAtiva(p); setMenuPlaylistsAberto(false); }}
                                                        className={`flex items-center gap-2 w-full px-4 py-2.5 text-sm transition-colors ${playlistAtiva?.Id === p.Id ? 'bg-blue-50 dark:bg-blue-900/20 text-blue-700 dark:text-blue-300' : 'text-slate-700 dark:text-slate-300 hover:bg-slate-50 dark:hover:bg-slate-700/50'}`}
                                                    >
                                                        <ListMusic size={15} className="shrink-0" />
                                                        <span className="truncate">{p.Name}</span>
                                                        {p.Tracks && p.Tracks.length > 0 && (
                                                            <span className="ml-auto text-xs text-slate-400 dark:text-slate-500">{p.Tracks.length}</span>
                                                        )}
                                                    </button>
                                                ))}
                                            </div>
                                        </>
                                    )}
                                </div>
                            )}
                        </div>
                        <button
                            onClick={toggleTheme}
                            title={theme === 'dark' ? 'Modo claro' : 'Modo escuro'}
                            className="inline-flex items-center justify-center w-10 h-10 rounded-lg text-slate-500 dark:text-slate-400 hover:text-slate-800 dark:hover:text-white bg-slate-200/70 dark:bg-slate-800/60 hover:bg-slate-200 dark:hover:bg-slate-800 transition-colors"
                        >
                            {theme === 'dark' ? <Sun size={18} /> : <Moon size={18} />}
                        </button>
                    </div>
                </header>

                {error && (
                    <div className="flex items-start gap-3 bg-red-500/10 border border-red-500/40 text-red-600 dark:text-red-300 rounded-lg px-4 py-3 mb-5 text-sm">
                        <AlertCircle size={16} className="shrink-0 mt-0.5" />
                        <span>{error}</span>
                    </div>
                )}

                <div className="mb-4">
                    <BuscadorMusicas onSelect={handleSelectBusca} />
                </div>

                <div className="mb-4">
                    <BreadcrumbMusicas path={path} onNavigate={navigateTo} />
                </div>

                {playlistAtiva && (
                    <div className="mb-4">
                        <PainelPlaylist onPlayTrack={setCurrentTrack} />
                    </div>
                )}

                <ListaMusicas
                    items={items}
                    loading={loading}
                    loadingMore={loadingMore}
                    hasMore={page < totalPages}
                    totalItems={totalItems}
                    isRoot={path === ''}
                    currentTrack={currentTrack}
                    sentinelRef={sentinelRef}
                    onSelect={handleSelect}
                    onUp={handleUp}
                />
            </div>

            {currentTrack && (
                <PlayerMusica
                    track={currentTrack}
                    onClose={() => setCurrentTrack(null)}
                    onPrev={() => irParaFaixa(-1)}
                    onNext={() => irParaFaixa(1)}
                    hasPrev={faixaAtualIdx > 0}
                    hasNext={faixaAtualIdx >= 0 && faixaAtualIdx < faixas.length - 1}
                />
            )}

            <CriarPlaylistDialog
                aberto={dialogCriarAberto}
                onFechar={() => setDialogCriarAberto(false)}
                onCriar={async (nome) => {
                    const nova = await criar(nome);
                    setPlaylistAtiva(nova);
                }}
            />
        </div>
    );
}

export function Musicas() {
    return (
        <PlaylistProvider>
            <MusicasInner />
        </PlaylistProvider>
    );
}
