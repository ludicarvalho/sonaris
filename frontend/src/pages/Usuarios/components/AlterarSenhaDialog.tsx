import { useEffect, useState } from 'react';
import { KeyRound, Loader2, X } from 'lucide-react';
import type { UserDto } from '../../../services/usuarios.service';

interface IAlterarSenhaDialog {
    aberto: boolean;
    usuario: UserDto | null;
    onFechar: () => void;
    onSalvar: (novaSenha: string) => Promise<void>;
}

const inputClasse =
    "w-full px-4 py-2.5 rounded-xl border border-slate-200 dark:border-slate-700 bg-white dark:bg-slate-800 text-sm text-slate-900 dark:text-white placeholder:text-slate-400 focus:outline-none focus:ring-2 focus:ring-blue-500/60 focus:border-blue-500 transition-shadow";

export function AlterarSenhaDialog({ aberto, usuario, onFechar, onSalvar }: IAlterarSenhaDialog) {
    const [senha, setSenha] = useState('');
    const [confirmarSenha, setConfirmarSenha] = useState('');
    const [erro, setErro] = useState<string | null>(null);
    const [salvando, setSalvando] = useState(false);

    useEffect(() => {
        if (aberto) {
            setSenha('');
            setConfirmarSenha('');
            setErro(null);
            setSalvando(false);
        }
    }, [aberto]);

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        if (senha.length < 4) {
            setErro('A senha deve ter pelo menos 4 caracteres.');
            return;
        }
        if (senha !== confirmarSenha) {
            setErro('As senhas não coincidem.');
            return;
        }
        setErro(null);
        setSalvando(true);
        try {
            if (usuario) {
                await onSalvar(senha);
            }
            onFechar();
        } catch (err: any) {
            setErro(err?.response?.data?.Message ?? err?.message ?? 'Não foi possível alterar a senha.');
        } finally {
            setSalvando(false);
        }
    };

    if (!aberto || !usuario) return null;

    const nomeExibido = usuario.NomeExibicao || usuario.Username;

    return (
        <div className="fixed inset-0 z-[60] flex items-center justify-center">
            <div className="absolute inset-0 bg-black/50" onClick={onFechar} />
            <div className="relative bg-white dark:bg-slate-800 rounded-2xl shadow-xl border border-slate-200 dark:border-slate-700 w-full max-w-sm mx-4 p-6">
                <div className="flex items-center justify-between mb-1">
                    <h2 className="text-lg font-semibold text-slate-900 dark:text-white flex items-center gap-2">
                        <KeyRound size={18} className="text-blue-600 dark:text-blue-400" />
                        Redefinir senha
                    </h2>
                    <button
                        onClick={onFechar}
                        className="inline-flex items-center justify-center w-8 h-8 rounded-lg text-slate-400 hover:text-slate-800 dark:hover:text-white hover:bg-slate-100 dark:hover:bg-slate-700 transition-colors"
                    >
                        <X size={18} />
                    </button>
                </div>
                <p className="text-sm text-slate-500 dark:text-slate-400 mb-4">
                    Defina uma nova senha para <span className="font-medium text-slate-700 dark:text-slate-200">{nomeExibido}</span>.
                </p>

                <form onSubmit={handleSubmit} className="space-y-4">
                    <div>
                        <label className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-1">
                            Nova senha <span className="text-red-500">*</span>
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
                            Confirmar nova senha <span className="text-red-500">*</span>
                        </label>
                        <input
                            type="password"
                            value={confirmarSenha}
                            onChange={(e) => setConfirmarSenha(e.target.value)}
                            autoComplete="new-password"
                            className={inputClasse}
                        />
                    </div>

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
                            {salvando ? <Loader2 size={15} className="animate-spin" /> : <KeyRound size={15} />}
                            {salvando ? 'Salvando...' : 'Salvar'}
                        </button>
                    </div>
                </form>
            </div>
        </div>
    );
}