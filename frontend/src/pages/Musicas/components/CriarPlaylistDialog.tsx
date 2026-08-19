import { useEffect, useRef } from 'react';
import { X } from 'lucide-react';

interface ICriarPlaylistDialog {
    aberto: boolean;
    onFechar: () => void;
    onCriar: (nome: string) => void;
}

export function CriarPlaylistDialog({ aberto, onFechar, onCriar }: ICriarPlaylistDialog) {
    const inputRef = useRef<HTMLInputElement | null>(null);

    useEffect(() => {
        if (aberto) {
            setTimeout(() => inputRef.current?.focus(), 50);
        }
    }, [aberto]);

    const handleSubmit = (e: React.FormEvent) => {
        e.preventDefault();
        const nome = inputRef.current?.value?.trim();
        if (nome) {
            onCriar(nome);
            if (inputRef.current) inputRef.current.value = '';
            onFechar();
        }
    };

    if (!aberto) return null;

    return (
        <div className="fixed inset-0 z-[60] flex items-center justify-center">
            <div className="absolute inset-0 bg-black/50" onClick={onFechar} />
            <div className="relative bg-white dark:bg-slate-800 rounded-2xl shadow-xl border border-slate-200 dark:border-slate-700 w-full max-w-sm mx-4 p-6">
                <div className="flex items-center justify-between mb-4">
                    <h2 className="text-lg font-semibold text-slate-900 dark:text-white">Nova Playlist</h2>
                    <button
                        onClick={onFechar}
                        className="inline-flex items-center justify-center w-8 h-8 rounded-lg text-slate-400 hover:text-slate-800 dark:hover:text-white hover:bg-slate-100 dark:hover:bg-slate-700 transition-colors"
                    >
                        <X size={18} />
                    </button>
                </div>

                <form onSubmit={handleSubmit}>
                    <input
                        ref={inputRef}
                        type="text"
                        placeholder="Nome da playlist"
                        maxLength={100}
                        className="w-full px-4 py-2.5 rounded-xl border border-slate-200 dark:border-slate-700 bg-white dark:bg-slate-800 text-sm text-slate-900 dark:text-white placeholder:text-slate-400 focus:outline-none focus:ring-2 focus:ring-blue-500/60 focus:border-blue-500 transition-shadow mb-4"
                        onKeyDown={(e) => {
                            if (e.key === 'Escape') onFechar();
                        }}
                    />
                    <div className="flex justify-end gap-2">
                        <button
                            type="button"
                            onClick={onFechar}
                            className="px-4 py-2 text-sm font-medium text-slate-600 dark:text-slate-400 hover:text-slate-800 dark:hover:text-white rounded-lg hover:bg-slate-100 dark:hover:bg-slate-700 transition-colors"
                        >
                            Cancelar
                        </button>
                        <button
                            type="submit"
                            className="px-4 py-2 text-sm font-medium text-white bg-blue-600 hover:bg-blue-700 rounded-lg transition-colors"
                        >
                            Criar
                        </button>
                    </div>
                </form>
            </div>
        </div>
    );
}
