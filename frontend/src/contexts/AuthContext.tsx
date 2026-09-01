import { useCallback, useEffect, useState, type ReactNode } from 'react';
import { AuthContext } from './useAuth';
import { clearAuth, getStoredUser, getToken, setAuth, type AuthUser } from '../services/auth.storage';
import { login as apiLogin, obterMe } from '../services/auth.service';

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<AuthUser | null>(() => getStoredUser());
  const [autenticando, setAutenticando] = useState(true);

  useEffect(() => {
    let active = true;

    const validar = async () => {
      if (!getStoredUser()) {
        if (active) setAutenticando(false);
        return;
      }
      try {
        const usuario = await obterMe();
        if (!active) return;
        if (usuario) {
          setUser(usuario);
          const token = getToken();
          if (token) setAuth(token, usuario);
        } else {
          clearAuth();
          setUser(null);
        }
      } catch {
        if (!active) return;
        clearAuth();
        setUser(null);
      } finally {
        if (active) setAutenticando(false);
      }
    };

    validar();

    return () => {
      active = false;
    };
  }, []);

  const login = useCallback(async (username: string, senha: string) => {
    const { token, user: novoUsuario } = await apiLogin(username, senha);
    setAuth(token, novoUsuario);
    setUser(novoUsuario);
  }, []);

  const logout = useCallback(() => {
    clearAuth();
    setUser(null);
  }, []);

  return (
    <AuthContext.Provider
      value={{
        user,
        isAdmin: user?.isAdmin ?? false,
        autenticando,
        login,
        logout,
      }}
    >
      {children}
    </AuthContext.Provider>
  );
}
