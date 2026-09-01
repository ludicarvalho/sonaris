import { http } from './http';
import type { BaseResponse } from '../types/api';
import type { AuthUser } from './auth.storage';

export interface LoginResponse {
  Token: string;
  User: AuthUser;
}

export const login = (username: string, senha: string) =>
  http.post<BaseResponse<LoginResponse>>('/api/Auth/Login', { username, senha });

export const obterMe = () =>
  http.get<BaseResponse<AuthUser>>('/api/Auth/Me');
