import { useCallback, useEffect, useState, type ReactNode } from 'react';
import { AuthContext } from './useAuth';
import { clearAuth, getStoredUser, setAuth, type AuthUser } from '../services/auth.storage';
import { login as apiLogin, obterMe } from '../services/auth.service';

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<AuthUser | null>(() => getStoredUser());
  const [autenticando, setAutenticando] = useState(true);

  useEffect(() => {
    let active = true;

    const validar = async () => {
      const tokenExistente = getStoredUser();
      if (!tokenExistente) {
        if (active) setAutenticando(false);
        return;
      }
      try {
        const { data } = await obterMe();
        if (!active) return;
        if (data.Success && data.Data) {
          setUser(data.Data);
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
    const { data } = await apiLogin(username, senha);
    if (!data.Success || !data.Data) {
      throw new Error(data.Message ?? 'Falha ao autenticar.');
    }
    setAuth(data.Data.Token, data.Data.User);
    setUser(data.Data.User);
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
