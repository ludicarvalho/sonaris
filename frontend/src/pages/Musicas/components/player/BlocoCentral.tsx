import { Pause, Play, SkipBack, SkipForward } from "lucide-react";
import { Tooltip } from "./Tooltip";
import { btnControle } from "./styles";

interface IBlocoCentral {
    tocando: boolean;
    hasPrev: boolean;
    hasNext: boolean;
    onPrev: () => void;
    onNext: () => void;
    onAlternarPlayPause: () => void;
}

export function BlocoCentral({
    tocando,
    hasPrev,
    hasNext,
    onPrev,
    onNext,
    onAlternarPlayPause,
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
                    <SkipBack size={20} />
                </button>
            </Tooltip>

                <Tooltip label={tocando ? "Pausar" : "Reproduzir"} shortcut="Espaço">
                    <button
                        onClick={onAlternarPlayPause}
                        className="flex items-center justify-center w-[50px] h-[50px] sm:w-11 sm:h-11 rounded-full bg-blue-600 hover:bg-blue-700 text-white transition-colors shrink-0"
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
                    <SkipForward size={20} />
                </button>
            </Tooltip>
            </div>
        </div>
    );
}
