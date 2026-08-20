import { useCallback, useState } from 'react';
import type { FileSystemItem } from '../types';

export function useTrackNavigation() {
    const [currentTrack, setCurrentTrack] = useState<FileSystemItem | null>(null);
    const [faixasPlaylist, setFaixasPlaylist] = useState<FileSystemItem[] | null>(null);

    const faixaAtualIdx = useCallback(
        (faixasAtivas: FileSystemItem[]) =>
            faixasAtivas.findIndex(f => f.RelativePath === currentTrack?.RelativePath),
        [currentTrack]
    );

    const irParaFaixa = useCallback(
        (faixasAtivas: FileSystemItem[], delta: number) => {
            const idx = faixasAtivas.findIndex(f => f.RelativePath === currentTrack?.RelativePath);
            const proxima = idx + delta;
            if (proxima >= 0 && proxima < faixasAtivas.length) {
                setCurrentTrack(faixasAtivas[proxima]);
            }
        },
        [currentTrack]
    );

    return {
        currentTrack,
        setCurrentTrack,
        faixasPlaylist,
        setFaixasPlaylist,
        faixaAtualIdx,
        irParaFaixa,
    };
}
