import { Pause, Play, SkipBack, SkipForward } from "lucide-react";
import { BarraProgresso } from "./BarraProgresso";
import { Tooltip } from "./Tooltip";
import { btnControle } from "./styles";

interface IBlocoCentral {
    tocando: boolean;
    tempoAtual: number;
    tempoTotal: number;
    progresso: number;
    buffer: number;
    hasPrev: boolean;
    hasNext: boolean;
    onPrev: () => void;
    onNext: () => void;
    onAlternarPlayPause: () => void;
    onBuscarPosicao: (e: React.MouseEvent<HTMLDivElement>) => void;
}

export function BlocoCentral({
    tocando,
    tempoAtual,
    tempoTotal,
    progresso,
    buffer,
    hasPrev,
    hasNext,
    onPrev,
    onNext,
    onAlternarPlayPause,
    onBuscarPosicao,
}: IBlocoCentral) {
    return (
        <div className="flex-1 flex flex-col items-center gap-1 min-w-0">
            <div className="flex items-center gap-2">
                <Tooltip label="Faixa anterior" shortcut="Num 4">
                <button
                    onClick={onPrev}
                    disabled={!hasPrev}
                    className={btnControle}
                >
                    <SkipBack size={18} />
                </button>
            </Tooltip>

                <Tooltip label={tocando ? "Pausar" : "Reproduzir"} shortcut="Espaço">
                    <button
                        onClick={onAlternarPlayPause}
                        className="flex items-center justify-center w-11 h-11 rounded-full bg-blue-600 hover:bg-blue-700 text-white transition-colors shrink-0"
                    >
                        {tocando ? <Pause size={20} /> : <Play size={20} className="ml-0.5" />}
                    </button>
                </Tooltip>

                <Tooltip label="Próxima faixa" shortcut="Num 6">
                <button
                    onClick={onNext}
                    disabled={!hasNext}
                    className={btnControle}
                >
                    <SkipForward size={18} />
                </button>
            </Tooltip>
            </div>

            <BarraProgresso
                tempoAtual={tempoAtual}
                tempoTotal={tempoTotal}
                progresso={progresso}
                buffer={buffer}
                onBuscarPosicao={onBuscarPosicao}
            />
        </div>
    );
}
