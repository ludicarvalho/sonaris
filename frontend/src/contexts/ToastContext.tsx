import { useCallback, useEffect, useRef, useState, type ReactNode } from 'react';
import { ToastContext, type ToastType } from './useToast';
import { ToastContainer } from '../components/ToastContainer';

const DURACAO_AUTO_FECHAR_MS = 3000;

interface ToastRegistrado {
    id: number;
    tipo: ToastType;
    mensagem: string;
    persistente: boolean;
}

export function ToastProvider({ children }: { children: ReactNode }) {
    const [toasts, setToasts] = useState<ToastRegistrado[]>([]);
    const proximoId = useRef(1);
    const timers = useRef(new Map<number, ReturnType<typeof setTimeout>>());

    const removerToast = useCallback((id: number) => {
        setToasts((prev) => prev.filter((t) => t.id !== id));
        const timer = timers.current.get(id);
        if (timer) {
            clearTimeout(timer);
            timers.current.delete(id);
        }
    }, []);

    const adicionarToast = useCallback(
        (tipo: ToastType, mensagem: string, persistente: boolean) => {
            const id = proximoId.current++;
            setToasts((prev) => [...prev.slice(-4), { id, tipo, mensagem, persistente }]);

            if (!persistente) {
                const timer = setTimeout(() => removerToast(id), DURACAO_AUTO_FECHAR_MS);
                timers.current.set(id, timer);
            }
        },
        [removerToast],
    );

    const success = useCallback((mensagem: string) => adicionarToast('success', mensagem, false), [adicionarToast]);
    const error = useCallback((mensagem: string) => adicionarToast('error', mensagem, true), [adicionarToast]);
    const info = useCallback((mensagem: string) => adicionarToast('info', mensagem, false), [adicionarToast]);

    useEffect(() => {
        const timersAtivos = timers.current;
        return () => {
            timersAtivos.forEach((timer) => clearTimeout(timer));
            timersAtivos.clear();
        };
    }, []);

    return (
        <ToastContext.Provider value={{ toasts, success, error, info, removerToast }}>
            {children}
            <ToastContainer />
        </ToastContext.Provider>
    );
}