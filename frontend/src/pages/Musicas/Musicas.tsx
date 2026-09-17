import { useEffect, useRef, useState } from 'react';
import { useSearchParams } from 'react-router-dom';
import { AlertCircle, Music4 } from 'lucide-react';
import type { FileSystemItem, MusicSearchResult } from './types';
import { arquivoDePath } from './types';
import { BreadcrumbMusicas } from './components/BreadcrumbMusicas';
import { BuscadorMusicas } from './components/BuscadorMusicas';
import { ListaMusicas } from './components/ListaMusicas';
import { PainelPlaylist } from './components/PainelPlaylist';
import { PlayerMusica } from './components/PlayerMusica';
import { CriarPlaylistDialog } from './components/CriarPlaylistDialog';
import { SidebarPlaylists } from './components/SidebarPlaylists';
import { PlaylistProvider } from '../../contexts/PlaylistContext';
import { usePageTitle } from '../../hooks/usePageTitle';
import { usePlaylist } from '../../hooks/usePlaylist';
import { AppShell } from '../../components/AppShell';
import { removerExensaoArquivo } from '../../utils/text';
import { useFileBrowser } from './hooks/useFileBrowser';
import { useTrackNavigation } from './hooks/useTrackNavigation';

function MusicasInner() {
    const { playlists, playlistAtiva, setPlaylistAtiva, criar } = usePlaylist();
    const [searchParams, setSearchParams] = useSearchParams();
    const [dialogCriarAberto, setDialogCriarAberto] = useState(false);
    const path = searchParams.get('path') ?? '';

    const { items, loading, loadingMore, hasMore, totalItems, error, sentinelRef, loadMore } = useFileBrowser(path);
    const { currentTrack, setCurrentTrack, listaAtiva, setListaAtiva, faixaAtualIdx, irParaFaixa } = useTrackNavigation();

    const tituloFaixa = currentTrack ? removerExensaoArquivo(currentTrack.Name) : undefined;
    usePageTitle(tituloFaixa);

    const navigateTo = (p: string) => {
        setSearchParams(p ? { path: p } : {}, { replace: false });
    };

    const faixas = items.filter(item => !item.IsDirectory);
    const emModoPasta = listaAtiva === null;
    const faixasAtivas = listaAtiva ?? faixas;
    const idx = faixaAtualIdx(faixasAtivas);
    const temProxima = idx >= 0 && (idx < faixasAtivas.length - 1 || (emModoPasta && hasMore && faixasAtivas.length > 0));

    // Quando a última faixa carregada da pasta termina e há mais páginas,
    // carrega a próxima página e só então avança para a faixa seguinte.
    const avancarPendenteRef = useRef(false);

    useEffect(() => {
        if (!avancarPendenteRef.current || loadingMore) return;
        const novoIdx = faixasAtivas.findIndex(f => f.RelativePath === currentTrack?.RelativePath);
        if (novoIdx >= 0 && novoIdx < faixasAtivas.length - 1) {
            avancarPendenteRef.current = false;
            irParaFaixa(faixasAtivas, 1);
        }
    }, [loadingMore, faixasAtivas, currentTrack, irParaFaixa]);

    const irParaProxima = () => {
        if (idx >= 0 && idx < faixasAtivas.length - 1) {
            avancarPendenteRef.current = false;
            irParaFaixa(faixasAtivas, 1);
            return;
        }
        if (emModoPasta && hasMore && !loadingMore) {
            avancarPendenteRef.current = true;
            loadMore();
        }
    };

    const handleSelect = (item: FileSystemItem) => {
        if (item.IsDirectory) {
            navigateTo(item.RelativePath);
        } else {
            setListaAtiva(null);
            setCurrentTrack(item);
        }
    };

    const handleSelectBusca = (resultados: MusicSearchResult[], item: MusicSearchResult) => {
        avancarPendenteRef.current = false;
        const idxSlash = item.RelativePath.lastIndexOf('/');
        const pasta = idxSlash > 0 ? item.RelativePath.slice(0, idxSlash) : '';
        navigateTo(pasta);
        setListaAtiva(resultados.map(r => arquivoDePath(r.RelativePath)));
        setCurrentTrack(arquivoDePath(item.RelativePath));
    };

    const handleUp = () => {
        navigateTo(path.split('/').slice(0, -1).join('/'));
    };

    return (
        <AppShell
            titulo="Músicas"
            subtitulo="Navegue pelas pastas e clique em uma faixa para tocar"
            icone={<Music4 size={24} className="text-white" />}
            sidebarExtra={(fechar) => (
                <SidebarPlaylists
                    playlists={playlists}
                    playlistAtiva={playlistAtiva}
                    onCriar={() => {
                        setDialogCriarAberto(true);
                        fechar();
                    }}
                    onSelecionar={(playlist) => {
                        setPlaylistAtiva(playlist);
                        fechar();
                    }}
                />
            )}
        >
            <div className={`max-w-4xl mx-auto px-4 pt-0 ${currentTrack ? 'pb-44' : 'pb-12'}`}>
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
                        <PainelPlaylist
                            currentTrack={currentTrack}
                            onPlayTrack={(item) => {
                                avancarPendenteRef.current = false;
                                setListaAtiva(playlistAtiva.Tracks.map(t => arquivoDePath(t.RelativePath)));
                                setCurrentTrack(item);
                            }}
                        />
                    </div>
                )}

                <ListaMusicas
                    items={items}
                    loading={loading}
                    loadingMore={loadingMore}
                    hasMore={hasMore}
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
                    onPrev={() => irParaFaixa(faixasAtivas, -1)}
                    onNext={irParaProxima}
                    hasPrev={idx > 0}
                    hasNext={temProxima}
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
        </AppShell>
    );
}

export function Musicas() {
    return (
        <PlaylistProvider>
            <MusicasInner />
        </PlaylistProvider>
    );
}