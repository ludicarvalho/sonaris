import { useEffect, useRef, useState } from "react";
import { lerMudoInicial, lerVolumeInicial, salvarMudo, salvarVolume } from "../utils";
import { streamUrl } from "../services/musicas.service";
import type { FileSystemItem } from "../types";

export function usePlayerAudio(track: FileSystemItem, hasPrev: boolean, hasNext: boolean, onPrev: () => void, onNext: () => void, onClose: () => void) {
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

    const pausar = () => {
        const audio = audioRef.current;
        if (!audio) return 0;
        audio.pause();
        return audio.currentTime;
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
            const alvo = e.target as HTMLElement | null;
            if (
                alvo &&
                (alvo instanceof HTMLInputElement ||
                 alvo instanceof HTMLTextAreaElement ||
                 alvo instanceof HTMLSelectElement ||
                 alvo.isContentEditable)
            ) {
                return;
            }

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
            if (e.code === 'Numpad4' && e.key === '4') {
                e.preventDefault();
                if (hasPrev) onPrev();
            }
            if (e.code === 'Numpad6' && e.key === '6') {
                e.preventDefault();
                if (hasNext) onNext();
            }
            if (e.key === 'Escape') onClose();
        };

        window.addEventListener('keydown', aoTeclar);
        return () => window.removeEventListener('keydown', aoTeclar);
    }, [hasPrev, hasNext, onPrev, onNext, onClose]);

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
        pausar,
        retomarAposEdicao,
        buscarPosicao,
        alterarVolume,
        alternarMudo,
    };
}
