import { useEffect, useRef, useState } from "react";
import { lerMudoInicial, lerVolumeInicial, salvarMudo, salvarVolume } from "../utils";
import { streamUrl } from "../services/musicas.service";
import type { FileSystemItem } from "../types";

export function usePlayerAudio(track: FileSystemItem, hasNext: boolean, onNext: () => void) {
    const audioRef = useRef<HTMLAudioElement | null>(null);
    const [tocando, setTocando] = useState(false);
    const [tempoAtual, setTempoAtual] = useState(0);
    const [tempoTotal, setTempoTotal] = useState(0);
    const [progresso, setProgresso] = useState(0);
    const [buffer, setBuffer] = useState(0);
    const [volume, setVolume] = useState<number>(lerVolumeInicial);
    const [mudo, setMudo] = useState<boolean>(lerMudoInicial);

    const alternarPlayPause = () => {
        const audio = audioRef.current;
        if (!audio) return;
        if (audio.paused) audio.play().catch(() => { });
        else audio.pause();
    };

    const atualizarTempo = () => {
        const audio = audioRef.current;
        if (!audio) return;
        setTempoAtual(audio.currentTime);
        if (audio.duration) setProgresso((audio.currentTime / audio.duration) * 100);
    };

    const aoCarregarDuracao = () => {
        const audio = audioRef.current;
        if (!audio) return;
        setTempoTotal(audio.duration || 0);
    };

    const atualizarBuffer = () => {
        const audio = audioRef.current;
        if (!audio || !audio.duration || !audio.buffered.length) return;
        setBuffer((audio.buffered.end(audio.buffered.length - 1) / audio.duration) * 100);
    };

    const buscarPosicao = (e: React.MouseEvent<HTMLDivElement>) => {
        const audio = audioRef.current;
        if (!audio) return;
        const rect = e.currentTarget.getBoundingClientRect();
        const frac = Math.min(1, Math.max(0, (e.clientX - rect.left) / rect.width));
        audio.currentTime = frac * audio.duration;
    };

    const alterarVolume = (e: React.ChangeEvent<HTMLInputElement>) => {
        const audio = audioRef.current;
        const v = parseFloat(e.target.value);
        setVolume(v);
        setMudo(v === 0);
        salvarVolume(v);
        if (v === 0) salvarMudo(true);
        if (!audio) return;
        audio.volume = v;
        audio.muted = false;
    };

    const alternarMudo = () => {
        const audio = audioRef.current;
        if (!audio) return;
        audio.muted = !audio.muted;
        setMudo(audio.muted);
        salvarMudo(audio.muted);
    };

    // Atalhos de teclado enquanto o tocador está aberto
    useEffect(() => {
        const aoTeclar = (e: KeyboardEvent) => {
            const audio = audioRef.current;
            if (!audio) return;
            if (e.code === 'Space') {
                e.preventDefault();
                if (audio.paused) audio.play().catch(() => { });
                else audio.pause();
            }
            if (e.key === 'ArrowRight') {
                e.preventDefault();
                audio.currentTime += 5;
            }
            if (e.key === 'ArrowLeft') {
                e.preventDefault();
                audio.currentTime -= 5;
            }
            if (e.key === 'ArrowUp') {
                e.preventDefault();
                audio.volume = Math.min(1, audio.volume + 0.1);
            }
            if (e.key === 'ArrowDown') {
                e.preventDefault();
                audio.volume = Math.max(0, audio.volume - 0.1);
            }
            if (e.key === 'm' || e.key === 'M') audio.muted = !audio.muted;
        };

        window.addEventListener('keydown', aoTeclar);
        return () => window.removeEventListener('keydown', aoTeclar);
    }, []);

    // Mantém o mesmo elemento <audio> (volume persiste) e só troca a fonte
    useEffect(() => {
        const audio = audioRef.current;
        if (!audio) return;

        audio.src = streamUrl(track.RelativePath);
        audio.volume = lerVolumeInicial();
        audio.muted = lerMudoInicial();
        audio.play().catch(() => { });
    }, [track]);

    const audioProps = {
        ref: audioRef,
        autoPlay: true,
        onPlay: () => setTocando(true),
        onPause: () => setTocando(false),
        onEnded: () => {
            setTocando(false);
            if (hasNext) onNext();
        },
        onTimeUpdate: atualizarTempo,
        onLoadedMetadata: aoCarregarDuracao,
        onProgress: atualizarBuffer,
        onVolumeChange: () => {
            const audio = audioRef.current;
            if (!audio) return;
            setVolume(audio.volume);
            setMudo(audio.muted);
            salvarVolume(audio.volume);
            salvarMudo(audio.muted);
        },
    };

    return {
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
    };
}
