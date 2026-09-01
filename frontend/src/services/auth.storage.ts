export interface AuthUser {
  id: string;
  username: string;
  nomeExibicao: string;
  isAdmin: boolean;
  createdAt?: string;
}

const TOKEN_KEY = 'sonaris.auth.token';
const USER_KEY = 'sonaris.auth.user';

export const getToken = (): string | null => localStorage.getItem(TOKEN_KEY);

export const setAuth = (token: string, user: AuthUser) => {
  localStorage.setItem(TOKEN_KEY, token);
  localStorage.setItem(USER_KEY, JSON.stringify(user));
};

export const getStoredUser = (): AuthUser | null => {
  const raw = localStorage.getItem(USER_KEY);
  if (!raw) return null;
  try {
    return JSON.parse(raw) as AuthUser;
  } catch {
    return null;
  }
};

export const clearAuth = () => {
  localStorage.removeItem(TOKEN_KEY);
  localStorage.removeItem(USER_KEY);
};
