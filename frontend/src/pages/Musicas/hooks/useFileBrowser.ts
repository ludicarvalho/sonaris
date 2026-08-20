import { useCallback, useEffect, useRef, useState } from 'react';
import { getMusicas } from '../services/musicas.service';
import type { FileSystemItem } from '../types';

const PAGE_SIZE = 30;

export function useFileBrowser(path: string) {
    const [items, setItems] = useState<FileSystemItem[]>([]);
    const [fetchState, setFetchState] = useState<'idle' | 'loading' | 'loadingMore'>('loading');
    const [error, setError] = useState('');
    const [page, setPage] = useState(1);
    const [totalPages, setTotalPages] = useState(1);
    const [totalItems, setTotalItems] = useState(0);
    const sentinelRef = useRef<HTMLDivElement | null>(null);

    useEffect(() => {
        let active = true;
        setFetchState('loading');
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
                if (active) setFetchState('idle');
            });

        return () => { active = false; };
    }, [path]);

    const loadMoreRef = useRef<() => void>(() => {});

    const loadMore = useCallback(() => {
        if (fetchState !== 'idle') return;
        if (page >= totalPages) return;

        setFetchState('loadingMore');
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
            .catch(() => {})
            .finally(() => {
                setFetchState('idle');
                requestAnimationFrame(() => {
                    const el = sentinelRef.current;
                    if (el && el.getBoundingClientRect().top <= window.innerHeight) {
                        loadMoreRef.current();
                    }
                });
            });
    }, [fetchState, page, totalPages, path]);

    loadMoreRef.current = loadMore;

    useEffect(() => {
        const el = sentinelRef.current;
        if (!el || fetchState === 'loading') return;

        const observer = new IntersectionObserver(
            (entries) => {
                if (entries[0].isIntersecting) loadMoreRef.current();
            },
            { rootMargin: '200px' },
        );
        observer.observe(el);

        return () => observer.disconnect();
    }, [fetchState, path]);

    return {
        items,
        loading: fetchState === 'loading',
        loadingMore: fetchState === 'loadingMore',
        hasMore: page < totalPages,
        totalItems,
        error,
        sentinelRef,
    };
}
