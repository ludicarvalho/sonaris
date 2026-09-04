import { CheckCircle2, Info, X, XCircle } from 'lucide-react';
import { useToast, type ToastType } from '../contexts/useToast';

const iconePorTipo: Record<ToastType, { Icone: typeof Info; cor: string; fundo: string }> = {
    success: { Icone: CheckCircle2, cor: 'text-emerald-500', fundo: 'bg-emerald-100 dark:bg-emerald-900/40' },
    error: { Icone: XCircle, cor: 'text-red-500', fundo: 'bg-red-100 dark:bg-red-900/40' },
    info: { Icone: Info, cor: 'text-blue-500', fundo: 'bg-blue-100 dark:bg-blue-900/40' },
};

export function ToastContainer() {
    const { toasts, removerToast } = useToast();

    if (toasts.length === 0) return null;

    return (
        <div className="fixed bottom-28 right-4 z-[70] flex flex-col items-end gap-2">
            {toasts.map((toast) => {
                const { Icone, cor, fundo } = iconePorTipo[toast.tipo];
                return (
                    <div
                        key={toast.id}
                        role="alert"
                        className="animate-toast-in max-w-96 flex items-center gap-3 bg-white dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-xl shadow-lg px-4 py-3"
                    >
                        <span className={`inline-flex items-center justify-center w-8 h-8 rounded-full shrink-0 ${fundo}`}>
                            <Icone size={18} className={cor} />
                        </span>
                        <span className="text-sm text-slate-700 dark:text-slate-300">{toast.mensagem}</span>
                        <button
                            onClick={() => removerToast(toast.id)}
                            aria-label="Fechar notificação"
                            className="ml-1 inline-flex items-center justify-center w-6 h-6 rounded-md text-slate-400 hover:text-slate-700 dark:hover:text-white hover:bg-slate-100 dark:hover:bg-slate-700 transition-colors shrink-0"
                        >
                            <X size={14} />
                        </button>
                    </div>
                );
            })}
        </div>
    );
}