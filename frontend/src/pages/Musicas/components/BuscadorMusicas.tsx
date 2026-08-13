import { useEffect, useRef, useState, type KeyboardEvent as ReactKeyboardEvent } from 'react';
import { Loader2, Music2, Search, X } from 'lucide-react';
import { buscarMusicasPorNome } from '../services/musicas.service';
import type { FileSystemItem } from '../types';
import { removerExensaoArquivo } from '../../../utils/text';

const DEBOUNCE_MS = 3000;

interface IBuscadorMusicas {
    onSelect: (item: FileSystemItem) => void;
}

export function BuscadorMusicas({ onSelect }: IBuscadorMusicas) {
    const [termo, setTermo] = useState('');
    const [resultados, setResultados] = useState<FileSystemItem[]>([]);
    const [buscando, setBuscando] = useState(false);
    const [pesquisado, setPesquisado] = useState(false);
    const [aberto, setAberto] = useState(false);
    const [indiceAtivo, setIndiceAtivo] = useState(-1);
    const containerRef = useRef<HTMLDivElement | null>(null);
    const itensRef = useRef<(HTMLLIElement | null)[]>([]);

    const limpar = () => {
        setTermo('');
        setResultados([]);
        setPesquisado(false);
        setBuscando(false);
        setAberto(false);
        setIndiceAtivo(-1);
    };

    // Fecha ao clicar fora do campo
    useEffect(() => {
        const aoClicarFora = (e: MouseEvent) => {
            if (containerRef.current && !containerRef.current.contains(e.target as Node)) {
                setAberto(false);
            }
        };

        document.addEventListener('mousedown', aoClicarFora);
        return () => document.removeEventListener('mousedown', aoClicarFora);
    }, []);

    // Fecha com Esc
    useEffect(() => {
        const aoTeclar = (e: KeyboardEvent) => {
            if (e.key === 'Escape') setAberto(false);
        };

        window.addEventListener('keydown', aoTeclar);
        return () => window.removeEventListener('keydown', aoTeclar);
    }, []);

    // Busca com debounce de 3s após parar de digitar
    useEffect(() => {
        const termoLimpo = termo.trim();

        if (!termoLimpo) {
            setResultados([]);
            setPesquisado(false);
            setBuscando(false);
            setAberto(false);
            setIndiceAtivo(-1);
            return;
        }

        setBuscando(true);
        setAberto(true);

        const controller = new AbortController();
        const timeout = setTimeout(() => {
            buscarMusicasPorNome(termoLimpo, controller.signal)
                .then((res) => {
                    const { Data, Success } = res.data;
                    const novos = Success ? (Data ?? []) : [];
                    setResultados(novos);
                    setIndiceAtivo(novos.length > 0 ? 0 : -1);
                    setPesquisado(true);
                })
                .catch((err: any) => {
                    if (err?.code !== 'ERR_CANCELED') {
                        setResultados([]);
                        setIndiceAtivo(-1);
                        setPesquisado(true);
                    }
                })
                .finally(() => {
                    if (!controller.signal.aborted) setBuscando(false);
                });
        }, DEBOUNCE_MS);

        return () => {
            clearTimeout(timeout);
            controller.abort();
        };
    }, [termo]);

    const aoSelecionar = (item: FileSystemItem) => {
        onSelect(item);
        limpar();
    };

    // Mantém o item ativo visível na área rolável do dropdown
    useEffect(() => {
        const el = itensRef.current[indiceAtivo];
        if (el) el.scrollIntoView({ block: 'nearest' });
    }, [indiceAtivo]);

    const aoNavegarTeclado = (e: ReactKeyboardEvent<HTMLInputElement>) => {
        const total = resultados.length;

        if (e.key === 'ArrowDown' || e.key === 'ArrowUp') {
            if (!aberto || total === 0) return;
            e.preventDefault();
            setIndiceAtivo((atual) => {
                if (e.key === 'ArrowDown') return atual >= total - 1 ? 0 : atual + 1;
                return atual <= 0 ? total - 1 : atual - 1;
            });
        }

        if (e.key === 'Enter') {
            if (!aberto || total === 0) return;
            e.preventDefault();
            aoSelecionar(resultados[indiceAtivo >= 0 ? indiceAtivo : 0]);
        }
    };

    const termoLimpo = termo.trim();
    const pastaDe = (item: FileSystemItem) => {
        const idx = item.RelativePath.lastIndexOf('/');
        return idx > 0 ? item.RelativePath.slice(0, idx) : 'Raiz';
    };

    return (
        <div ref={containerRef} className="relative">
            <div className="relative">
                <Search size={18} className="absolute left-3 top-1/2 -translate-y-1/2 text-slate-400 dark:text-slate-500" />
                <input
                    type="text"
                    value={termo}
                    onChange={(e) => setTermo(e.target.value)}
                    onFocus={() => {
                        if (termoLimpo) setAberto(true);
                    }}
                    onKeyDown={aoNavegarTeclado}
                    placeholder="Buscar música pelo nome..."
                    className="w-full pl-10 pr-10 py-2.5 rounded-xl border border-slate-200 dark:border-slate-700 bg-white dark:bg-slate-800 text-sm text-slate-900 dark:text-white placeholder:text-slate-400 dark:placeholder:text-slate-500 focus:outline-none focus:ring-2 focus:ring-blue-500/60 focus:border-blue-500 transition-shadow"
                />
                {termo && (
                    <button
                        onClick={limpar}
                        title="Limpar busca"
                        className="absolute right-2.5 top-1/2 -translate-y-1/2 inline-flex items-center justify-center w-7 h-7 rounded-md text-slate-400 dark:text-slate-500 hover:text-slate-700 dark:hover:text-slate-200 hover:bg-slate-100 dark:hover:bg-slate-700 transition-colors"
                    >
                        <X size={16} />
                    </button>
                )}
            </div>

            {aberto && termoLimpo && (
                <div className="absolute inset-x-0 top-full mt-1.5 z-20 bg-white dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-xl shadow-lg overflow-hidden max-h-72 overflow-y-auto">
                    {buscando ? (
                        <div className="flex items-center justify-center gap-2 px-4 py-6 text-sm text-slate-400 dark:text-slate-500">
                            <Loader2 size={16} className="animate-spin text-blue-500" />
                            Buscando músicas...
                        </div>
                    ) : pesquisado && resultados.length === 0 ? (
                        <div className="px-4 py-6 text-sm text-slate-500 dark:text-slate-400">
                            Nenhuma música encontrada para &ldquo;{termoLimpo}&rdquo;. Verifique a ortografia ou tente outro termo.
                        </div>
                    ) : (
                        <ul className="divide-y divide-slate-100 dark:divide-slate-700">
                            {resultados.map((item, index) => (
                                <li
                                    key={item.RelativePath}
                                    ref={(el) => {
                                        itensRef.current[index] = el;
                                    }}
                                >
                                    <button
                                        onClick={() => aoSelecionar(item)}
                                        onMouseEnter={() => setIndiceAtivo(index)}
                                        className={`w-full flex items-center gap-3 px-4 py-2.5 text-left transition-colors ${index === indiceAtivo
                                            ? 'bg-slate-100 dark:bg-slate-700/50'
                                            : 'hover:bg-slate-50 dark:hover:bg-slate-700/50'
                                            }`}
                                    >
                                        <Music2 size={16} className="shrink-0 text-blue-500" />
                                        <span className="min-w-0">
                                            <span className="block truncate font-medium text-slate-700 dark:text-slate-300">
                                                {removerExensaoArquivo(item.Name)}
                                            </span>
                                            <span className="block truncate text-xs text-slate-400 dark:text-slate-500">
                                                {pastaDe(item)}
                                            </span>
                                        </span>
                                    </button>
                                </li>
                            ))}
                        </ul>
                    )}
                </div>
            )}
        </div>
    );
}