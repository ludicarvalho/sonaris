import { useCallback, useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  AlertCircle,
  ArrowLeft,
  KeyRound,
  Loader2,
  Moon,
  ShieldCheck,
  ShieldOff,
  Sun,
  UserPlus,
  Users,
} from 'lucide-react';
import { useAuth } from '../../contexts/useAuth';
import { usePageTitle } from '../../hooks/usePageTitle';
import { useTheme } from '../../contexts/useTheme';
import { formatarData } from '../../utils/text';
import {
  alterarPapel,
  alterarSenha,
  listarUsuarios,
  registrarUsuario,
  type RegistrarUsuarioParams,
  type UserDto,
} from '../../services/usuarios.service';
import { CriarUsuarioDialog } from './components/CriarUsuarioDialog';
import { AlterarSenhaDialog } from './components/AlterarSenhaDialog';

const seletorIcone =
    "inline-flex items-center justify-center w-10 h-10 rounded-lg text-slate-500 dark:text-slate-400 hover:text-slate-800 dark:hover:text-white bg-slate-200/70 dark:bg-slate-800/60 hover:bg-slate-200 dark:hover:bg-slate-800 transition-colors";

export function Usuarios() {
    const navigate = useNavigate();
    const { theme, toggleTheme } = useTheme();
    const { user } = useAuth();
    usePageTitle('Usuários');

    const [usuarios, setUsuarios] = useState<UserDto[]>([]);
    const [loading, setLoading] = useState(true);
    const [erro, setErro] = useState<string | null>(null);
    const [sucesso, setSucesso] = useState<string | null>(null);
    const [operando, setOperando] = useState<string | null>(null);
    const [dialogCriarAberto, setDialogCriarAberto] = useState(false);
    const [senhaUsuario, setSenhaUsuario] = useState<UserDto | null>(null);

    const carregar = useCallback(async () => {
        setLoading(true);
        setErro(null);
        try {
            const resposta = await listarUsuarios();
            setUsuarios(resposta.data.Data ?? []);
        } catch (err: any) {
            setErro(err?.response?.data?.Message ?? err?.message ?? 'Não foi possível carregar os usuários.');
        } finally {
            setLoading(false);
        }
    }, []);

    useEffect(() => {
        carregar();
    }, [carregar]);

    const mostrarSucesso = (mensagem: string) => {
        setSucesso(mensagem);
        window.setTimeout(() => setSucesso(null), 3000);
    };

    const criarUsuario = async (params: RegistrarUsuarioParams) => {
        await registrarUsuario(params);
        mostrarSucesso('Usuário criado com sucesso.');
        await carregar();
    };

    const atualizarPapel = async (usuario: UserDto) => {
        if (usuario.Id === user?.id) return;
        const novoPapel = !usuario.IsAdmin;
        const nome = usuario.NomeExibicao || usuario.Username;
        const acao = novoPapel ? 'promover a administrador' : 'remover o acesso de administrador';
        if (!confirm(`Deseja ${acao} o usuário "${nome}"?`)) return;

        setOperando(usuario.Id);
        setErro(null);
        try {
            await alterarPapel(usuario.Id, novoPapel);
            setUsuarios(prev => prev.map(u => (u.Id === usuario.Id ? { ...u, IsAdmin: novoPapel } : u)));
            mostrarSucesso(novoPapel ? `${nome} agora é administrador.` : `${nome} deixou de ser administrador.`);
        } catch (err: any) {
            setErro(err?.response?.data?.Message ?? err?.message ?? 'Não foi possível alterar o papel.');
        } finally {
            setOperando(null);
        }
    };

    const salvarSenha = async (id: string, novaSenha: string) => {
        await alterarSenha(id, novaSenha);
        mostrarSucesso('Senha alterada com sucesso.');
    };

    const inicial = (u: UserDto) => (u.NomeExibicao || u.Username).trim().charAt(0).toUpperCase() || '?';

    return (
        <div className="min-h-screen bg-gradient-to-br from-slate-100 via-slate-50 to-blue-50 text-slate-900 dark:from-slate-900 dark:via-slate-900 dark:to-blue-950 dark:text-white">
            <div className="max-w-4xl mx-auto px-4 py-8">
                <header className="flex items-start justify-between gap-4 mb-6">
                    <div className="flex items-center gap-4">
                        <div className="flex items-center justify-center w-12 h-12 rounded-2xl bg-gradient-to-br from-blue-500 to-indigo-600 shadow-lg shadow-blue-600/40 shrink-0">
                            <Users size={24} className="text-white" />
                        </div>
                        <div>
                            <h1 className="text-2xl font-bold">Usuários</h1>
                            <p className="text-slate-500 dark:text-slate-400 text-sm">Gerencie contas e permissões de acesso</p>
                        </div>
                    </div>
                    <div className="flex items-center gap-2 shrink-0">
                        <button
                            onClick={() => navigate('/musicas')}
                            title="Voltar para músicas"
                            className={seletorIcone}
                        >
                            <ArrowLeft size={18} />
                        </button>
                        <button
                            onClick={toggleTheme}
                            title={theme === 'dark' ? 'Modo claro' : 'Modo escuro'}
                            className={seletorIcone}
                        >
                            {theme === 'dark' ? <Sun size={18} /> : <Moon size={18} />}
                        </button>
                    </div>
                </header>

                <div className="flex items-center justify-between gap-4 mb-4">
                    <p className="text-sm text-slate-500 dark:text-slate-400">
                        {loading ? 'Carregando...' : `${usuarios.length} usuário${usuarios.length === 1 ? '' : 's'} cadastrado${usuarios.length === 1 ? '' : 's'}`}
                    </p>
                    <button
                        onClick={() => setDialogCriarAberto(true)}
                        className="inline-flex items-center gap-1.5 px-3 py-2 text-sm font-medium text-white bg-blue-600 hover:bg-blue-700 rounded-lg transition-colors"
                    >
                        <UserPlus size={16} />
                        Novo usuário
                    </button>
                </div>

                {erro && (
                    <div className="flex items-start gap-3 bg-red-500/10 border border-red-500/40 text-red-600 dark:text-red-300 rounded-lg px-4 py-3 mb-4 text-sm">
                        <AlertCircle size={16} className="shrink-0 mt-0.5" />
                        <span>{erro}</span>
                    </div>
                )}

                {sucesso && (
                    <div className="bg-emerald-500/10 border border-emerald-500/40 text-emerald-700 dark:text-emerald-300 rounded-lg px-4 py-3 mb-4 text-sm">
                        {sucesso}
                    </div>
                )}

                <div className="bg-white dark:bg-slate-800 rounded-xl border border-slate-200 dark:border-slate-700 shadow-sm overflow-hidden">
                    {loading ? (
                        <div className="divide-y divide-slate-100 dark:divide-slate-700">
                            {[0, 1, 2, 3, 4].map((i) => (
                                <div key={i} className="flex items-center gap-3 px-4 py-4">
                                    <div className="w-10 h-10 rounded-full bg-slate-100 dark:bg-slate-700 animate-pulse" />
                                    <div className="flex-1 space-y-2">
                                        <div className="h-3.5 w-40 bg-slate-100 dark:bg-slate-700 rounded animate-pulse" />
                                        <div className="h-3 w-24 bg-slate-100 dark:bg-slate-700 rounded animate-pulse" />
                                    </div>
                                    <div className="h-6 w-16 bg-slate-100 dark:bg-slate-700 rounded animate-pulse" />
                                </div>
                            ))}
                        </div>
                    ) : usuarios.length === 0 ? (
                        <p className="px-4 py-10 text-center text-sm text-slate-500 dark:text-slate-400">
                            Nenhum usuário cadastrado.
                        </p>
                    ) : (
                        <div className="divide-y divide-slate-100 dark:divide-slate-700">
                            {usuarios.map((u) => {
                                const ehVoce = u.Id === user?.id;
                                const nome = u.NomeExibicao || u.Username;
                                return (
                                    <div key={u.Id} className="flex items-center gap-3 px-4 py-3.5">
                                        <div className="flex items-center justify-center w-10 h-10 rounded-full bg-blue-100 dark:bg-blue-900/40 text-blue-700 dark:text-blue-300 font-semibold text-sm shrink-0">
                                            {inicial(u)}
                                        </div>
                                        <div className="min-w-0 flex-1">
                                            <p className="font-medium text-slate-900 dark:text-white truncate flex items-center gap-2">
                                                <span className="truncate">{nome}</span>
                                                {ehVoce && (
                                                    <span className="shrink-0 text-[10px] font-semibold uppercase tracking-wide text-blue-600 dark:text-blue-400 bg-blue-100 dark:bg-blue-900/40 rounded px-1.5 py-0.5">
                                                        você
                                                    </span>
                                                )}
                                            </p>
                                            <p className="text-xs text-slate-500 dark:text-slate-400 truncate">
                                                @{u.Username}
                                            </p>
                                        </div>
                                        <div className="hidden sm:block text-xs text-slate-400 dark:text-slate-500 w-28 shrink-0">
                                            {formatarData(u.CreatedAt)}
                                        </div>
                                        <div className="shrink-0">
                                            {u.IsAdmin ? (
                                                <span className="inline-flex items-center gap-1 px-2 py-1 rounded-md bg-blue-100 dark:bg-blue-900/40 text-blue-700 dark:text-blue-300 text-xs font-medium">
                                                    <ShieldCheck size={13} />
                                                    Admin
                                                </span>
                                            ) : (
                                                <span className="inline-flex items-center px-2 py-1 rounded-md bg-slate-100 dark:bg-slate-700 text-slate-600 dark:text-slate-300 text-xs font-medium">
                                                    Usuário
                                                </span>
                                            )}
                                        </div>
                                        <div className="flex items-center gap-1 shrink-0">
                                            <button
                                                onClick={() => atualizarPapel(u)}
                                                disabled={ehVoce || operando === u.Id}
                                                title={ehVoce ? 'Você não pode remover o próprio acesso de admin' : u.IsAdmin ? 'Remover acesso de administrador' : 'Tornar administrador'}
                                                className={`inline-flex items-center justify-center w-9 h-9 rounded-lg text-slate-500 dark:text-slate-400 hover:text-blue-600 dark:hover:text-blue-400 bg-slate-100 dark:bg-slate-700/50 hover:bg-blue-50 dark:hover:bg-blue-900/20 transition-colors disabled:opacity-40 disabled:cursor-not-allowed`}
                                            >
                                                {operando === u.Id ? (
                                                    <Loader2 size={16} className="animate-spin" />
                                                ) : u.IsAdmin ? (
                                                    <ShieldOff size={16} />
                                                ) : (
                                                    <ShieldCheck size={16} />
                                                )}
                                            </button>
                                            <button
                                                onClick={() => setSenhaUsuario(u)}
                                                title={`Redefinir senha de ${nome}`}
                                                className="inline-flex items-center justify-center w-9 h-9 rounded-lg text-slate-500 dark:text-slate-400 hover:text-blue-600 dark:hover:text-blue-400 bg-slate-100 dark:bg-slate-700/50 hover:bg-blue-50 dark:hover:bg-blue-900/20 transition-colors"
                                            >
                                                <KeyRound size={16} />
                                            </button>
                                        </div>
                                    </div>
                                );
                            })}
                        </div>
                    )}
                </div>
            </div>

            <CriarUsuarioDialog
                aberto={dialogCriarAberto}
                onFechar={() => setDialogCriarAberto(false)}
                onCriar={criarUsuario}
            />

            <AlterarSenhaDialog
                aberto={senhaUsuario !== null}
                usuario={senhaUsuario}
                onFechar={() => setSenhaUsuario(null)}
                onSalvar={(novaSenha) => senhaUsuario ? salvarSenha(senhaUsuario.Id, novaSenha) : Promise.resolve()}
            />
        </div>
    );
}