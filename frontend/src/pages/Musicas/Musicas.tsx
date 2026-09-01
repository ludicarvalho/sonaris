import { useState } from 'react';
import { useSearchParams } from 'react-router-dom';
import { AlertCircle, Music4 } from 'lucide-react';
import type { FileSystemItem } from './types';
import { arquivoDePath } from './types';
import { pastaDe } from './utils';
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

    const { items, loading, loadingMore, hasMore, totalItems, error, sentinelRef } = useFileBrowser(path);
    const { currentTrack, setCurrentTrack, faixasPlaylist, setFaixasPlaylist, faixaAtualIdx, irParaFaixa } = useTrackNavigation();

    const tituloFaixa = currentTrack ? removerExensaoArquivo(currentTrack.Name) : undefined;
    usePageTitle(tituloFaixa);

    const navigateTo = (p: string) => {
        setSearchParams(p ? { path: p } : {}, { replace: false });
    };

    const faixas = items.filter(item => !item.IsDirectory);
    const faixasAtivas = faixasPlaylist ?? faixas;
    const idx = faixaAtualIdx(faixasAtivas);

    const handleSelect = (item: FileSystemItem) => {
        if (item.IsDirectory) {
            navigateTo(item.RelativePath);
        } else {
            setFaixasPlaylist(null);
            setCurrentTrack(item);
        }
    };

    const handleSelectBusca = (relativePath: string) => {
        navigateTo(pastaDe(relativePath));
        setFaixasPlaylist(null);
        setCurrentTrack(arquivoDePath(relativePath));
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
                    onCriar={() => setDialogCriarAberto(true)}
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
                                setFaixasPlaylist(playlistAtiva.Tracks.map(t => arquivoDePath(t.RelativePath)));
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
                    onNext={() => irParaFaixa(faixasAtivas, 1)}
                    hasPrev={idx > 0}
                    hasNext={idx >= 0 && idx < faixasAtivas.length - 1}
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