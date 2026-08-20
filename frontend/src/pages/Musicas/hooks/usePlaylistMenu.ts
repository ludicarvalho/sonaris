import { useRef, useState } from 'react';
import { useClickOutside } from '../../../hooks/useClickOutside';

export function usePlaylistMenu() {
    const [aberto, setAberto] = useState(false);
    const [dialogCriarAberto, setDialogCriarAberto] = useState(false);
    const menuRef = useRef<HTMLDivElement | null>(null);

    useClickOutside(menuRef, () => setAberto(false));

    return {
        aberto,
        setAberto,
        dialogCriarAberto,
        setDialogCriarAberto,
        menuRef,
    };
}
