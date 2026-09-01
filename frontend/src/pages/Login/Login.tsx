import { useState, type FormEvent } from 'react';
import { Navigate } from 'react-router-dom';
import { useAuth } from '../../contexts/useAuth';

function Login() {
  const { login, user, autenticando } = useAuth();
  const [username, setUsername] = useState('');
  const [senha, setSenha] = useState('');
  const [erro, setErro] = useState<string | null>(null);
  const [enviando, setEnviando] = useState(false);

  if (!autenticando && user) {
    return <Navigate to="/musicas" replace />;
  }

  const aoEnviar = async (e: FormEvent) => {
    e.preventDefault();
    if (!username || !senha) {
      setErro('Informe usuário e senha.');
      return;
    }
    setErro(null);
    setEnviando(true);
    try {
      await login(username, senha);
    } catch (err) {
      setErro(err instanceof Error ? err.message : 'Falha ao entrar.');
    } finally {
      setEnviando(false);
    }
  };

  return (
    <div className="min-h-screen flex items-center justify-center bg-slate-100 dark:bg-slate-900 px-4">
      <div className="w-full max-w-sm bg-white dark:bg-slate-800 rounded-2xl shadow-lg p-8 border border-slate-200 dark:border-slate-700">
        <h1 className="text-2xl font-bold text-slate-900 dark:text-white mb-1">Sonaris</h1>
        <p className="text-sm text-slate-500 dark:text-slate-400 mb-6">Entre com sua conta para continuar</p>

        <form onSubmit={aoEnviar} className="space-y-4">
          <div>
            <label className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-1">
              Usuário
            </label>
            <input
              type="text"
              value={username}
              onChange={(e) => setUsername(e.target.value)}
              autoComplete="username"
              className="w-full px-3 py-2 rounded-lg border border-slate-300 dark:border-slate-600 bg-white dark:bg-slate-900 text-slate-900 dark:text-white focus:outline-none focus:ring-2 focus:ring-blue-500"
            />
          </div>

          <div>
            <label className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-1">
              Senha
            </label>
            <input
              type="password"
              value={senha}
              onChange={(e) => setSenha(e.target.value)}
              autoComplete="current-password"
              className="w-full px-3 py-2 rounded-lg border border-slate-300 dark:border-slate-600 bg-white dark:bg-slate-900 text-slate-900 dark:text-white focus:outline-none focus:ring-2 focus:ring-blue-500"
            />
          </div>

          {erro && (
            <p className="text-sm text-red-600 dark:text-red-400">{erro}</p>
          )}

          <button
            type="submit"
            disabled={enviando}
            className="w-full py-2 rounded-lg bg-blue-600 hover:bg-blue-700 disabled:opacity-50 text-white font-medium transition-colors"
          >
            {enviando ? 'Entrando…' : 'Entrar'}
          </button>
        </form>
      </div>
    </div>
  );
}

export default Login;
