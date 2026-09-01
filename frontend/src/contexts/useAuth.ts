import { createContext, useContext } from 'react';
import type { AuthUser } from '../services/auth.storage';

export interface AuthContextValue {
  user: AuthUser | null;
  isAdmin: boolean;
  autenticando: boolean;
  login: (username: string, senha: string) => Promise<void>;
  logout: () => void;
}

export const AuthContext = createContext<AuthContextValue>({
  user: null,
  isAdmin: false,
  autenticando: true,
  login: async () => { },
  logout: () => { },
});

export const useAuth = () => useContext(AuthContext);
