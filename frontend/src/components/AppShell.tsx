import { useEffect, useState, type ReactNode } from 'react';
import { useLocation, useNavigate } from 'react-router-dom';
import { LogOut, Menu, Moon, Music4, Sun, Users, X } from 'lucide-react';
import { useAuth } from '../contexts/useAuth';
import { useTheme } from '../contexts/useTheme';

interface IAppShell {
    titulo: string;
    subtitulo: string;
    icone: ReactNode;
    acoes?: ReactNode;
    sidebarExtra?: (fechar: () => void) => ReactNode;
    children: ReactNode;
}

const botaoIcone =
    "inline-flex items-center justify-center w-10 h-10 rounded-lg text-slate-500 dark:text-slate-400 hover:text-slate-800 dark:hover:text-white bg-slate-200/70 dark:bg-slate-800/60 hover:bg-slate-200 dark:hover:bg-slate-800 transition-colors";

export function AppShell({ titulo, subtitulo, icone, acoes, sidebarExtra, children }: IAppShell) {
    const { theme, toggleTheme } = useTheme();
    const { user, isAdmin, logout } = useAuth();
    const navigate = useNavigate();
    const { pathname } = useLocation();
    const [aberto, setAberto] = useState(false);

    useEffect(() => {
        setAberto(false);
    }, [pathname]);

    useEffect(() => {
        if (!aberto) return;
        const handleKey = (e: KeyboardEvent) => {
            if (e.key === 'Escape') setAberto(false);
        };
        window.addEventListener('keydown', handleKey);
        return () => window.removeEventListener('keydown', handleKey);
    }, [aberto]);

    const fechar = () => setAberto(false);

    const irPara = (rota: string) => {
        navigate(rota);
        fechar();
    };

    const navItemBase =
        "flex items-center gap-2.5 w-full px-3 py-2 rounded-lg text-sm font-medium transition-colors";
    const navItemInativo = "text-slate-600 dark:text-slate-300 hover:bg-slate-100 dark:hover:bg-slate-700/50";
    const navItemAtivo = "text-blue-700 dark:text-blue-300 bg-blue-50 dark:bg-blue-900/20";

    return (
        <div className="min-h-screen bg-gradient-to-br from-slate-100 via-slate-50 to-blue-50 text-slate-900 dark:from-slate-900 dark:via-slate-900 dark:to-blue-950 dark:text-white">
            <div className="max-w-4xl mx-auto px-4 pt-8">
                <header className="flex items-center justify-between gap-4 mb-6">
                    <div className="flex items-center gap-4">
                        <button onClick={() => setAberto(true)} title="Menu" className={botaoIcone}>
                            <Menu size={18} />
                        </button>
                        <div className="flex items-center justify-center w-12 h-12 rounded-2xl bg-gradient-to-br from-blue-500 to-indigo-600 shadow-lg shadow-blue-600/40 shrink-0">
                            {icone}
                        </div>
                        <div>
                            <h1 className="text-2xl font-bold">{titulo}</h1>
                            <p className="text-slate-500 dark:text-slate-400 text-sm">{subtitulo}</p>
                        </div>
                    </div>
                    {acoes && <div className="flex items-center gap-2 shrink-0">{acoes}</div>}
                </header>
            </div>

            {children}

            <div className={`fixed inset-0 z-[60] ${aberto ? '' : 'pointer-events-none'}`}>
                <div
                    className={`absolute inset-0 bg-black/50 transition-opacity ${aberto ? 'opacity-100' : 'opacity-0'}`}
                    onClick={fechar}
                />
                <aside
                    className={`absolute left-0 top-0 h-full w-[18rem] max-w-[85vw] bg-white dark:bg-slate-800 border-r border-slate-200 dark:border-slate-700 shadow-xl flex flex-col transition-transform duration-200 ${aberto ? 'translate-x-0' : '-translate-x-full'}`}
                >
                    <div className="flex items-center justify-between px-4 py-4 border-b border-slate-100 dark:border-slate-700">
                        <div className="flex items-center gap-2">
                            <div className="flex items-center justify-center w-9 h-9 rounded-xl bg-gradient-to-br from-blue-500 to-indigo-600">
                                <Music4 size={18} className="text-white" />
                            </div>
                            <span className="text-lg font-bold">Sonaris</span>
                        </div>
                        <button
                            onClick={fechar}
                            title="Fechar menu"
                            className="inline-flex items-center justify-center w-8 h-8 rounded-lg text-slate-400 hover:text-slate-800 dark:hover:text-white hover:bg-slate-100 dark:hover:bg-slate-700 transition-colors"
                        >
                            <X size={18} />
                        </button>
                    </div>

                    <nav className="px-3 py-3 space-y-1">
                        <button
                            onClick={() => irPara('/musicas')}
                            className={`${navItemBase} ${pathname === '/musicas' ? navItemAtivo : navItemInativo}`}
                        >
                            <Music4 size={16} />
                            Músicas
                        </button>
                        {isAdmin && (
                            <button
                                onClick={() => irPara('/usuarios')}
                                className={`${navItemBase} ${pathname === '/usuarios' ? navItemAtivo : navItemInativo}`}
                            >
                                <Users size={16} />
                                Usuários
                            </button>
                        )}
                    </nav>

                    {sidebarExtra && (
                        <div className="flex-1 min-h-0 overflow-y-auto border-t border-slate-100 dark:border-slate-700">
                            {sidebarExtra(fechar)}
                        </div>
                    )}

                    <div className="mt-auto border-t border-slate-100 dark:border-slate-700 px-3 py-3">
                        <div className="flex items-center gap-2">
                            <button
                                onClick={toggleTheme}
                                title={theme === 'dark' ? 'Modo claro' : 'Modo escuro'}
                                className="inline-flex items-center justify-center w-10 h-10 rounded-lg text-slate-500 dark:text-slate-400 hover:text-slate-800 dark:hover:text-white bg-slate-100 dark:bg-slate-700/50 hover:bg-slate-200 dark:hover:bg-slate-700 transition-colors"
                            >
                                {theme === 'dark' ? <Sun size={18} /> : <Moon size={18} />}
                            </button>
                            {user && (
                                <span className="flex-1 min-w-0 text-sm text-slate-500 dark:text-slate-400 truncate">
                                    {user.nomeExibicao || user.username}
                                </span>
                            )}
                            <button
                                onClick={logout}
                                title="Sair"
                                className="inline-flex items-center justify-center w-10 h-10 rounded-lg text-slate-500 dark:text-slate-400 hover:text-red-600 dark:hover:text-red-400 bg-slate-100 dark:bg-slate-700/50 hover:bg-red-50 dark:hover:bg-red-900/20 transition-colors"
                            >
                                <LogOut size={18} />
                            </button>
                        </div>
                    </div>
                </aside>
            </div>
        </div>
    );
}