import { useCallback, useState } from 'react';
import type { FileSystemItem } from '../types';

export function useTrackNavigation() {
    const [currentTrack, setCurrentTrack] = useState<FileSystemItem | null>(null);
    const [listaAtiva, setListaAtiva] = useState<FileSystemItem[] | null>(null);

    const faixaAtualIdx = useCallback(
        (faixas: FileSystemItem[]) =>
            faixas.findIndex(f => f.RelativePath === currentTrack?.RelativePath),
        [currentTrack]
    );

    const irParaFaixa = useCallback(
        (faixas: FileSystemItem[], delta: number) => {
            const idx = faixas.findIndex(f => f.RelativePath === currentTrack?.RelativePath);
            const proxima = idx + delta;
            if (proxima >= 0 && proxima < faixas.length) {
                setCurrentTrack(faixas[proxima]);
            }
        },
        [currentTrack]
    );

    return {
        currentTrack,
        setCurrentTrack,
        listaAtiva,
        setListaAtiva,
        faixaAtualIdx,
        irParaFaixa,
    };
}