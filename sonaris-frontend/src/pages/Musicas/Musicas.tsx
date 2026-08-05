import { useCallback, useEffect, useRef, useState } from 'react';
import { useSearchParams } from 'react-router-dom';
import { AlertCircle, Moon, Music4, Sun } from 'lucide-react';
import { getMusicas } from './services/musicas.service';
import type { FileSystemItem } from './types';
import { BreadcrumbMusicas } from './components/BreadcrumbMusicas';
import { ListaMusicas } from './components/ListaMusicas';
import { PlayerMusica } from './components/PlayerMusica';
import { usePageTitle } from '../../hooks/usePageTitle';
import { useTheme } from '../../contexts/useTheme';

const PAGE_SIZE = 30;

export function Musicas() {
  usePageTitle();
  const { theme, toggleTheme } = useTheme();
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
  const sentinelRef = useRef<HTMLDivElement | null>(null);

  const navigateTo = (p: string) => {
    setSearchParams(p ? { path: p } : {}, { replace: false });
  };

  // Carrega a primeira página ao montar ou trocar de diretório
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
        // Se a sentinela continuar visível (tela alta), carrega a próxima página
        requestAnimationFrame(() => {
          const el = sentinelRef.current;
          if (el && el.getBoundingClientRect().top <= window.innerHeight) {
            loadMoreRef.current();
          }
        });
      });
  }, [loading, loadingMore, page, totalPages, path]);

  loadMoreRef.current = loadMore;

  // Observa a sentinela no fim da lista para carregar mais ao rolar
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
          <button
            onClick={toggleTheme}
            title={theme === 'dark' ? 'Modo claro' : 'Modo escuro'}
            className="inline-flex items-center justify-center w-10 h-10 rounded-lg text-slate-500 dark:text-slate-400 hover:text-slate-800 dark:hover:text-white bg-slate-200/70 dark:bg-slate-800/60 hover:bg-slate-200 dark:hover:bg-slate-800 transition-colors shrink-0"
          >
            {theme === 'dark' ? <Sun size={18} /> : <Moon size={18} />}
          </button>
        </header>

        {error && (
          <div className="flex items-start gap-3 bg-red-500/10 border border-red-500/40 text-red-600 dark:text-red-300 rounded-lg px-4 py-3 mb-5 text-sm">
            <AlertCircle size={16} className="shrink-0 mt-0.5" />
            <span>{error}</span>
          </div>
        )}

        <div className="mb-4">
          <BreadcrumbMusicas path={path} onNavigate={navigateTo} />
        </div>

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
    </div>
  );
}
