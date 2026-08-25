import { useEffect, useRef, useState } from "react";
import { lerMudoInicial, lerVolumeInicial, salvarMudo, salvarVolume } from "../utils";
import { streamUrl } from "../services/musicas.service";
import type { FileSystemItem } from "../types";
import { usePlayerKeyboard } from "./usePlayerKeyboard";

export function usePlayerAudio(track: FileSystemItem, hasPrev: boolean, hasNext: boolean, onPrev: () => void, onNext: () => void, onClose: () => void) {
    const audioRef = useRef<HTMLAudioElement | null>(null);
    const [tocando, setTocando] = useState(false);
    const [tempoAtual, setTempoAtual] = useState(0);
    const [tempoTotal, setTempoTotal] = useState(0);
    const [progresso, setProgresso] = useState(0);
    const [buffer, setBuffer] = useState(0);
    const [volume, setVolume] = useState<number>(lerVolumeInicial);
    const [mudo, setMudo] = useState<boolean>(lerMudoInicial);

    usePlayerKeyboard({ audioRef, hasPrev, hasNext, onPrev, onNext, onClose });

    const alternarPlayPause = () => {
        const audio = audioRef.current;
        if (!audio) return;
        if (audio.paused) audio.play().catch(() => { });
        else audio.pause();
    };

    const pausar = () => {
        const audio = audioRef.current;
        if (!audio) return 0;
        audio.pause();
        return audio.currentTime;
    };

    const reproduzir = () => {
        audioRef.current?.play().catch(() => { });
    };

    const retomarAposEdicao = (posicao: number, deveTocar: boolean) => {
        const audio = audioRef.current;
        if (!audio) return;

        const aplicar = () => {
            if (isFinite(posicao)) audio.currentTime = posicao;
            if (deveTocar) audio.play().catch(() => { });
        };

        audio.addEventListener('loadedmetadata', aplicar, { once: true });
        audio.load();
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

    const buscarPosicao = (e: React.PointerEvent<HTMLDivElement>) => {
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

    // Mantém o mesmo elemento <audio> (volume persiste) e só troca a fonte
    useEffect(() => {
        const audio = audioRef.current;
        if (!audio) return;

        audio.src = streamUrl(track.RelativePath);
        audio.volume = volume;
        audio.muted = mudo;
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
        reproduzir,
        pausar,
        retomarAposEdicao,
        buscarPosicao,
        alterarVolume,
        alternarMudo,
    };
}
