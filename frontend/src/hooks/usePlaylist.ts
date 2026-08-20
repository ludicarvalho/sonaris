import { useContext } from 'react';
import { PlaylistContext, type PlaylistContextType } from '../contexts/PlaylistContext';

export function usePlaylist(): PlaylistContextType {
    const ctx = useContext(PlaylistContext);
    if (!ctx) throw new Error('usePlaylist deve ser usado dentro de PlaylistProvider');
    return ctx;
}
