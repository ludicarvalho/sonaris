import { useEffect, useRef, useState } from 'react';
import { Loader2, ShieldCheck, UserPlus, X } from 'lucide-react';
import type { RegistrarUsuarioParams } from '../../../services/usuarios.service';

interface ICriarUsuarioDialog {
    aberto: boolean;
    onFechar: () => void;
    onCriar: (params: RegistrarUsuarioParams) => Promise<void>;
}

const inputClasse =
    "w-full px-4 py-2.5 rounded-xl border border-slate-200 dark:border-slate-700 bg-white dark:bg-slate-800 text-sm text-slate-900 dark:text-white placeholder:text-slate-400 focus:outline-none focus:ring-2 focus:ring-blue-500/60 focus:border-blue-500 transition-shadow";

export function CriarUsuarioDialog({ aberto, onFechar, onCriar }: ICriarUsuarioDialog) {
    const usernameRef = useRef<HTMLInputElement | null>(null);
    const [username, setUsername] = useState('');
    const [nomeExibicao, setNomeExibicao] = useState('');
    const [senha, setSenha] = useState('');
    const [confirmarSenha, setConfirmarSenha] = useState('');
    const [isAdmin, setIsAdmin] = useState(false);
    const [erro, setErro] = useState<string | null>(null);
    const [salvando, setSalvando] = useState(false);

    useEffect(() => {
        if (aberto) {
            setUsername('');
            setNomeExibicao('');
            setSenha('');
            setConfirmarSenha('');
            setIsAdmin(false);
            setErro(null);
            setSalvando(false);
            setTimeout(() => usernameRef.current?.focus(), 50);
        }
    }, [aberto]);

    const validar = (): string | null => {
        const u = username.trim();
        if (!u) return 'Informe o nome de usuário.';
        if (senha.length < 4) return 'A senha deve ter pelo menos 4 caracteres.';
        if (senha !== confirmarSenha) return 'As senhas não coincidem.';
        return null;
    };

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        const problema = validar();
        if (problema) {
            setErro(problema);
            return;
        }
        setErro(null);
        setSalvando(true);
        try {
            await onCriar({
                Username: username.trim(),
                Senha: senha,
                NomeExibicao: nomeExibicao.trim(),
                IsAdmin: isAdmin,
            });
            onFechar();
        } catch (err: any) {
            setErro(err?.response?.data?.Message ?? err?.message ?? 'Não foi possível criar o usuário.');
        } finally {
            setSalvando(false);
        }
    };

    if (!aberto) return null;

    return (
        <div className="fixed inset-0 z-[60] flex items-center justify-center">
            <div className="absolute inset-0 bg-black/50" onClick={onFechar} />
            <div className="relative bg-white dark:bg-slate-800 rounded-2xl shadow-xl border border-slate-200 dark:border-slate-700 w-full max-w-md mx-4 p-6">
                <div className="flex items-center justify-between mb-4">
                    <h2 className="text-lg font-semibold text-slate-900 dark:text-white flex items-center gap-2">
                        <UserPlus size={18} className="text-blue-600 dark:text-blue-400" />
                        Novo usuário
                    </h2>
                    <button
                        onClick={onFechar}
                        className="inline-flex items-center justify-center w-8 h-8 rounded-lg text-slate-400 hover:text-slate-800 dark:hover:text-white hover:bg-slate-100 dark:hover:bg-slate-700 transition-colors"
                    >
                        <X size={18} />
                    </button>
                </div>

                <form onSubmit={handleSubmit} className="space-y-4">
                    <div>
                        <label className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-1">
                            Usuário <span className="text-red-500">*</span>
                        </label>
                        <input
                            ref={usernameRef}
                            type="text"
                            value={username}
                            onChange={(e) => setUsername(e.target.value)}
                            placeholder="ex.: joao.silva"
                            maxLength={50}
                            autoComplete="off"
                            className={inputClasse}
                        />
                    </div>

                    <div>
                        <label className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-1">
                            Nome de exibição
                        </label>
                        <input
                            type="text"
                            value={nomeExibicao}
                            onChange={(e) => setNomeExibicao(e.target.value)}
                            placeholder="Nome exibido no cabeçalho"
                            maxLength={100}
                            className={inputClasse}
                        />
                    </div>

                    <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
                        <div>
                            <label className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-1">
                                Senha <span className="text-red-500">*</span>
                            </label>
                            <input
                                type="password"
                                value={senha}
                                onChange={(e) => setSenha(e.target.value)}
                                autoComplete="new-password"
                                className={inputClasse}
                            />
                        </div>
                        <div>
                            <label className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-1">
                                Confirmar senha <span className="text-red-500">*</span>
                            </label>
                            <input
                                type="password"
                                value={confirmarSenha}
                                onChange={(e) => setConfirmarSenha(e.target.value)}
                                autoComplete="new-password"
                                className={inputClasse}
                            />
                        </div>
                    </div>

                    <label className="flex items-center gap-2 text-sm text-slate-700 dark:text-slate-300 cursor-pointer select-none">
                        <input
                            type="checkbox"
                            checked={isAdmin}
                            onChange={(e) => setIsAdmin(e.target.checked)}
                            className="accent-blue-600 w-4 h-4"
                        />
                        <ShieldCheck size={15} className="text-blue-600 dark:text-blue-400" />
                        Administrador (pode editar músicas e gerenciar usuários)
                    </label>

                    {erro && (
                        <p className="text-sm text-red-600 dark:text-red-300 bg-red-500/10 border border-red-500/40 rounded-lg px-3 py-2">
                            {erro}
                        </p>
                    )}

                    <div className="flex justify-end gap-2">
                        <button
                            type="button"
                            onClick={onFechar}
                            disabled={salvando}
                            className="px-4 py-2 text-sm font-medium text-slate-600 dark:text-slate-400 hover:text-slate-800 dark:hover:text-white rounded-lg hover:bg-slate-100 dark:hover:bg-slate-700 transition-colors disabled:opacity-50"
                        >
                            Cancelar
                        </button>
                        <button
                            type="submit"
                            disabled={salvando}
                            className="inline-flex items-center gap-1.5 px-4 py-2 text-sm font-medium text-white bg-blue-600 hover:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed rounded-lg transition-colors"
                        >
                            {salvando ? <Loader2 size={15} className="animate-spin" /> : <UserPlus size={15} />}
                            {salvando ? 'Criando...' : 'Criar'}
                        </button>
                    </div>
                </form>
            </div>
        </div>
    );
}