import { Volume1, Volume2, VolumeX, X } from "lucide-react";
import { Tooltip } from "./Tooltip";
import { btnExtra } from "./styles";

interface IBlocoDireita {
    volume: number;
    mudo: boolean;
    onAlternarMudo: () => void;
    onAlterarVolume: (e: React.ChangeEvent<HTMLInputElement>) => void;
    onClose: () => void;
}

export function BlocoDireita({ volume, mudo, onAlternarMudo, onAlterarVolume, onClose }: IBlocoDireita) {
    return (
        <div className="flex items-center justify-end gap-1 sm:gap-2 shrink-0">
            <Tooltip label={mudo || volume === 0 ? "Ativar som" : "Mudo"} shortcut="M">
                <button
                    onClick={onAlternarMudo}
                    className={btnExtra}
                >
                    {mudo || volume === 0 ? <VolumeX size={18} /> : volume < 0.5 ? <Volume1 size={18} /> : <Volume2 size={18} />}
                </button>
            </Tooltip>
            <Tooltip label="Volume" shortcut="↑ ↓" wrapperClassName="max-w-full shrink-0">
                <input
                    type="range"
                    min="0"
                    max="1"
                    step="0.01"
                    value={mudo ? 0 : volume}
                    onChange={onAlterarVolume}
                    className="w-20 sm:w-24 accent-blue-600 cursor-pointer hidden sm:block pointer-events-auto"
                />
            </Tooltip>
            <button
                onClick={onClose}
                title="Fechar tocador"
                className={btnExtra}
            >
                <X size={18} />
            </button>
        </div>
    );
}
