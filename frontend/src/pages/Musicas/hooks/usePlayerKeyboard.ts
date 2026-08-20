import { useEffect, type RefObject } from 'react';

interface UsePlayerKeyboardProps {
    audioRef: RefObject<HTMLAudioElement | null>;
    hasPrev: boolean;
    hasNext: boolean;
    onPrev: () => void;
    onNext: () => void;
    onClose: () => void;
}

export function usePlayerKeyboard({ audioRef, hasPrev, hasNext, onPrev, onNext, onClose }: UsePlayerKeyboardProps) {
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
                if (audio.paused) audio.play().catch(() => {});
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
    }, [audioRef, hasPrev, hasNext, onPrev, onNext, onClose]);
}
