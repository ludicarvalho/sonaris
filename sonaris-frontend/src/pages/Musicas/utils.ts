export function formatarTamanho(bytes: number | null): string {
  if (bytes == null || bytes <= 0) return '—';

  const unidades = ['B', 'KB', 'MB', 'GB'];
  const indice = Math.min(Math.floor(Math.log(bytes) / Math.log(1024)), unidades.length - 1);
  const valor = bytes / Math.pow(1024, indice);

  return `${valor.toFixed(indice === 0 ? 0 : 1)} ${unidades[indice]}`;
}

export function formatarData(iso: string): string {
  if (!iso) return '—';

  return new Intl.DateTimeFormat('pt-BR', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
  }).format(new Date(iso));
}
