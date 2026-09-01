import { http } from './http';
import type { BaseResponse } from '../types/api';
import type { AuthUser } from './auth.storage';
import type { UserDto } from './usuarios.service';

export interface LoginResponse {
  Token: string;
  User: AuthUser;
}

function usuarioParaAuthUser(usuario: UserDto): AuthUser {
  return {
    id: usuario.Id,
    username: usuario.Username,
    nomeExibicao: usuario.NomeExibicao ?? '',
    isAdmin: usuario.IsAdmin,
    createdAt: usuario.CreatedAt,
  };
}

export const login = async (username: string, senha: string) => {
  const { data } = await http.post<BaseResponse<{ Token: string; User: UserDto }>>('/api/Auth/Login', { username, senha });
  if (!data.Success || !data.Data) {
    throw new Error(data.Message ?? 'Falha ao autenticar.');
  }
  return { token: data.Data.Token, user: usuarioParaAuthUser(data.Data.User) };
};

export const obterMe = async () => {
  const { data } = await http.get<BaseResponse<UserDto>>('/api/Auth/Me');
  if (!data.Success || !data.Data) {
    throw new Error(data.Message ?? 'Falha ao obter o usuário.');
  }
  return usuarioParaAuthUser(data.Data);
};
