export function removerExensaoArquivo(nomeArquivo: string): string {
  return nomeArquivo.replace(/\.[^/.]+$/, '');
}
