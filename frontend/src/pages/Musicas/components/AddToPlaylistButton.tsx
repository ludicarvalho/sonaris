import { useState } from 'react';
import { ListPlus } from 'lucide-react';
import { AdicionarPlaylistMenu } from './AdicionarPlaylistMenu';
import { CriarPlaylistDialog } from './CriarPlaylistDialog';
import { usePlaylist } from '../../../hooks/usePlaylist';

interface IAddToPlaylistButton {
    relativePath: string;
    className?: string;
}

export function AddToPlaylistButton({ relativePath, className }: IAddToPlaylistButton) {
    const { criar } = usePlaylist();
    const [menuAberto, setMenuAberto] = useState(false);
    const [dialogAberto, setDialogAberto] = useState(false);

    return (
        <div className={className}>
            <button
                onClick={(e) => {
                    e.stopPropagation();
                    setMenuAberto(!menuAberto);
                }}
                title="Adicionar à playlist"
                className="inline-flex items-center justify-center w-7 h-7 rounded-md text-slate-400 dark:text-slate-500 hover:text-blue-500 hover:bg-blue-50 dark:hover:bg-blue-900/20 transition-colors"
            >
                <ListPlus size={15} />
            </button>
            <AdicionarPlaylistMenu
                relativePath={relativePath}
                aberto={menuAberto}
                onFechar={() => setMenuAberto(false)}
                onCriarPlaylist={() => {
                    setMenuAberto(false);
                    setDialogAberto(true);
                }}
            />
            <CriarPlaylistDialog
                aberto={dialogAberto}
                onFechar={() => setDialogAberto(false)}
                onCriar={async (nome) => { await criar(nome); }}
            />
        </div>
    );
}
