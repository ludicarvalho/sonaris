import { createContext, useContext } from 'react';

export type ToastType = 'success' | 'error' | 'info';

export interface ToastData {
    id: number;
    tipo: ToastType;
    mensagem: string;
}

export interface ToastContextType {
    toasts: ToastData[];
    success: (mensagem: string) => void;
    error: (mensagem: string) => void;
    info: (mensagem: string) => void;
    removerToast: (id: number) => void;
}

export const ToastContext = createContext<ToastContextType | null>(null);

export function useToast() {
    const context = useContext(ToastContext);
    if (!context) {
        throw new Error('useToast deve ser usado dentro de um ToastProvider.');
    }
    return context;
}