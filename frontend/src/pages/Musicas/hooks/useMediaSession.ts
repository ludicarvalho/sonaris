import { useEffect, useRef } from 'react';

interface IUseMediaSession {
    titulo: string;
    artista: string;
    album: string;
    capaUrl: string | null;
    tocando: boolean;
    onReproduzir: () => void;
    onPausar: () => void;
    onPrev: () => void;
    onNext: () => void;
}

function temSuporte(): boolean {
    return typeof navigator !== 'undefined' && 'mediaSession' in navigator;
}

export function useMediaSession({
    titulo,
    artista,
    album,
    capaUrl,
    tocando,
    onReproduzir,
    onPausar,
    onPrev,
    onNext,
}: IUseMediaSession) {
    const acoesRef = useRef({ onReproduzir, onPausar, onPrev, onNext });

    useEffect(() => {
        acoesRef.current = { onReproduzir, onPausar, onPrev, onNext };
    });

    // Limpa os metadados ao desmontar o player
    useEffect(() => {
        if (!temSuporte()) return;

        return () => {
            navigator.mediaSession.metadata = null;
        };
    }, []);

    // Atualiza os metadados (título, artista, álbum e capa) conforme a faixa
    useEffect(() => {
        if (!temSuporte()) return;

        navigator.mediaSession.metadata = new MediaMetadata({
            title: titulo || undefined,
            artist: artista || undefined,
            album: album || undefined,
            artwork: capaUrl ? [{ src: capaUrl }] : [],
        });
    }, [titulo, artista, album, capaUrl]);

    // Registra os controles de mídia (tela bloqueada / notificação)
    useEffect(() => {
        if (!temSuporte()) return;

        const ms = navigator.mediaSession;
        ms.setActionHandler('play', () => acoesRef.current.onReproduzir());
        ms.setActionHandler('pause', () => acoesRef.current.onPausar());
        ms.setActionHandler('previoustrack', () => acoesRef.current.onPrev());
        ms.setActionHandler('nexttrack', () => acoesRef.current.onNext());

        return () => {
            ms.setActionHandler('play', null);
            ms.setActionHandler('pause', null);
            ms.setActionHandler('previoustrack', null);
            ms.setActionHandler('nexttrack', null);
        };
    }, []);

    // Reflete o estado de reprodução no sistema
    useEffect(() => {
        if (!temSuporte()) return;
        navigator.mediaSession.playbackState = tocando ? 'playing' : 'paused';
    }, [tocando]);
}
