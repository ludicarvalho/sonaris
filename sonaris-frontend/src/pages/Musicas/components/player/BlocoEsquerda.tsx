import { Capa } from "./Capa";

interface IBlocoEsquerda {
    capaUrl: string | null;
    titulo: string;
    subtitulo: string;
    expandido: boolean;
    onAlternarExpandido: () => void;
}

export function BlocoEsquerda({ capaUrl, titulo, subtitulo, expandido, onAlternarExpandido }: IBlocoEsquerda) {
    return (
        <div
            onClick={onAlternarExpandido}
            title={expandido ? "Recolher detalhes" : "Expandir detalhes"}
            className="flex items-center gap-3 min-w-0 cursor-pointer select-none"
        >
            <Capa capaUrl={capaUrl} />
            <div className="min-w-0">
                <p className="text-sm font-semibold text-slate-800 dark:text-slate-100 truncate">{titulo}</p>
                <p className="text-xs text-slate-500 dark:text-slate-400 truncate">
                    {subtitulo}
                </p>
            </div>
        </div>
    );
}
