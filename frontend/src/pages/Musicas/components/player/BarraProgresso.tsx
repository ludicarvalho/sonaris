import { formatarTempo } from "../../utils";
import { Tooltip } from "./Tooltip";

interface IBarraProgresso {
    tempoAtual: number;
    tempoTotal: number;
    progresso: number;
    buffer: number;
    onBuscarPosicao: (e: React.MouseEvent<HTMLDivElement>) => void;
}

export function BarraProgresso({ tempoAtual, tempoTotal, progresso, buffer, onBuscarPosicao }: IBarraProgresso) {
    return (
        <div className="flex items-center gap-2 w-full">
            <span className="text-[11px] text-slate-500 dark:text-slate-400 tabular-nums shrink-0">
                {formatarTempo(tempoAtual)}
            </span>

            <Tooltip label="Buscar posição" shortcut="← →" wrapperClassName="flex-1 min-w-0">
                <div
                    onClick={onBuscarPosicao}
                    className="relative flex-1 h-1.5 bg-slate-200 dark:bg-slate-700 rounded-full cursor-pointer"
                >
                    <div
                        className="absolute inset-y-0 left-0 rounded-full bg-slate-400 dark:bg-slate-500"
                        style={{ width: `${buffer}%` }}
                    />
                    <div
                        className="absolute inset-y-0 left-0 rounded-full bg-blue-600"
                        style={{ width: `${progresso}%` }}
                    />
                    <div
                        className="absolute top-1/2 -translate-y-1/2 w-3 h-3 rounded-full bg-blue-600 border-2 border-white dark:border-slate-800 shadow"
                        style={{ left: `calc(${progresso}% - 6px)` }}
                    />
                </div>
            </Tooltip>

            <span className="text-[11px] text-slate-500 dark:text-slate-400 tabular-nums shrink-0">
                {formatarTempo(tempoTotal)}
            </span>
        </div>
    );
}
