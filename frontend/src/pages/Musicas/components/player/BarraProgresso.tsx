import { useRef } from "react";
import { formatarTempo } from "../../utils";
import { Tooltip } from "./Tooltip";

interface IBarraProgresso {
    tempoAtual: number;
    tempoTotal: number;
    progresso: number;
    buffer: number;
    onBuscarPosicao: (e: React.PointerEvent<HTMLDivElement>) => void;
}

export function BarraProgresso({ tempoAtual, tempoTotal, progresso, buffer, onBuscarPosicao }: IBarraProgresso) {
    const arrastandoRef = useRef(false);

    const aoPointerDown = (e: React.PointerEvent<HTMLDivElement>) => {
        e.currentTarget.setPointerCapture(e.pointerId);
        arrastandoRef.current = true;
        onBuscarPosicao(e);
    };

    const aoPointerMove = (e: React.PointerEvent<HTMLDivElement>) => {
        if (!arrastandoRef.current) return;
        onBuscarPosicao(e);
    };

    const aoPointerUp = (e: React.PointerEvent<HTMLDivElement>) => {
        e.currentTarget.releasePointerCapture(e.pointerId);
        arrastandoRef.current = false;
    };

    return (
        <div className="flex items-center gap-2 w-full sm:max-w-4xl sm:mx-auto">
            <span className="text-[11px] text-slate-500 dark:text-slate-400 tabular-nums shrink-0">
                {formatarTempo(tempoAtual)}
            </span>

            <Tooltip label="Buscar posição" shortcut="← →" wrapperClassName="flex-1 min-w-0">
                <div
                    onPointerDown={aoPointerDown}
                    onPointerMove={aoPointerMove}
                    onPointerUp={aoPointerUp}
                    onPointerCancel={aoPointerUp}
                    className="relative flex-1 h-1.5 bg-slate-200 dark:bg-slate-700 rounded-full cursor-pointer touch-none"
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
