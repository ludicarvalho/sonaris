import { useEffect, useState } from "react";
import { getCapaMusica, getMusicaMetadata } from "../services/musicas.service";
import type { FileSystemItem, MusicMetadata } from "../types";
import { removerExensaoArquivo } from "../../../utils/text";
import { usePlayerAudio } from "../hooks/usePlayerAudio";
import { BlocoCentral } from "./player/BlocoCentral";
import { BlocoDireita } from "./player/BlocoDireita";
import { BlocoEsquerda } from "./player/BlocoEsquerda";
import { DetalhesExpandidos } from "./player/DetalhesExpandidos";

interface IPlayerMusica {
    track: FileSystemItem;
    onClose: () => void;
    onPrev: () => void;
    onNext: () => void;
    hasPrev: boolean;
    hasNext: boolean;
}

export function PlayerMusica({ track, onClose, onPrev, onNext, hasPrev, hasNext }: IPlayerMusica) {
    const {
        audioProps,
        tocando,
        tempoAtual,
        tempoTotal,
        progresso,
        buffer,
        volume,
        mudo,
        alternarPlayPause,
        buscarPosicao,
        alterarVolume,
        alternarMudo,
    } = usePlayerAudio(track, hasNext, onNext);

    const [metadata, setMetadata] = useState<MusicMetadata | null>(null);
    const [capaUrl, setCapaUrl] = useState<string | null>(null);
    const [expandido, setExpandido] = useState(false);

    // Busca os metadados (título, artista, álbum, duração, bitrate, etc.)
    useEffect(() => {
        let active = true;
        setMetadata(null);

        getMusicaMetadata(track.RelativePath)
            .then((res) => {
                if (!active) return;
                const { Success, Data } = res.data;
                if (Success && Data) setMetadata(Data);
            })
            .catch(() => { });

        return () => {
            active = false;
        };
    }, [track]);

    // Busca a capa (fallback para o ícone quando a API devolve erro)
    useEffect(() => {
        let active = true;
        let objectUrl: string | null = null;
        setCapaUrl(null);

        getCapaMusica(track.RelativePath)
            .then((res) => {
                if (!active) return;
                objectUrl = URL.createObjectURL(res.data);
                setCapaUrl(objectUrl);
            })
            .catch(() => {
                if (active) setCapaUrl(null);
            });

        return () => {
            active = false;
            if (objectUrl) URL.revokeObjectURL(objectUrl);
        };
    }, [track]);

    const titulo = metadata?.Title || removerExensaoArquivo(track.Name);
    const artistaAlbum = [metadata?.Artist, metadata?.Album].filter(Boolean).join(" • ");

    return (
        <div className="fixed bottom-0 left-0 right-0 z-50 bg-white dark:bg-slate-800 border-t border-slate-200 dark:border-slate-700 shadow-[0_-8px_24px_rgba(0,0,0,0.08)]">
            {expandido && (
                <DetalhesExpandidos
                    capaUrl={capaUrl}
                    titulo={titulo}
                    artistaAlbum={artistaAlbum}
                    metadata={metadata}
                    track={track}
                />
            )}

            <div className="px-4 py-3 grid grid-cols-[1fr_clamp(320px,40vw,32rem)_1fr] items-center gap-4">
                <BlocoEsquerda
                    capaUrl={capaUrl}
                    titulo={titulo}
                    subtitulo={artistaAlbum || track.RelativePath}
                    expandido={expandido}
                    onAlternarExpandido={() => setExpandido(v => !v)}
                />

                <BlocoCentral
                    tocando={tocando}
                    tempoAtual={tempoAtual}
                    tempoTotal={tempoTotal}
                    progresso={progresso}
                    buffer={buffer}
                    hasPrev={hasPrev}
                    hasNext={hasNext}
                    onPrev={onPrev}
                    onNext={onNext}
                    onAlternarPlayPause={alternarPlayPause}
                    onBuscarPosicao={buscarPosicao}
                />

                <BlocoDireita
                    volume={volume}
                    mudo={mudo}
                    onAlternarMudo={alternarMudo}
                    onAlterarVolume={alterarVolume}
                    onClose={onClose}
                />
            </div>

            <audio
                {...audioProps}
                className="hidden"
            />
        </div>
    );
}
