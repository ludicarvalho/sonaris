import { http } from './http';
import type { BaseResponse } from '../types/api';

export interface UserDto {
  Id: string;
  Username: string;
  NomeExibicao: string;
  IsAdmin: boolean;
  CreatedAt: string;
}

export interface RegistrarUsuarioParams {
  Username: string;
  Senha: string;
  NomeExibicao: string;
  IsAdmin: boolean;
}

export const listarUsuarios = () =>
  http.get<BaseResponse<UserDto[]>>('/api/Auth/Usuarios');

export const registrarUsuario = (params: RegistrarUsuarioParams) =>
  http.post<BaseResponse<UserDto>>('/api/Auth/Registrar', params);

export const alterarPapel = (id: string, isAdmin: boolean) =>
  http.put<BaseResponse<object>>(`/api/Auth/Usuarios/${id}/papel`, isAdmin);

export const alterarSenha = (id: string, novaSenha: string) =>
  http.put<BaseResponse<object>>(`/api/Auth/Usuarios/${id}/senha`, novaSenha);