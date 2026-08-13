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

export function formatarTempo(seg: number): string {
  if (!seg || Number.isNaN(seg)) return '0:00';

  const horas = Math.floor(seg / 3600);
  const minutos = Math.floor((seg % 3600) / 60);
  const segundos = Math.floor(seg % 60);
  const ss = String(segundos).padStart(2, '0');

  return horas > 0 ? `${horas}:${String(minutos).padStart(2, '0')}:${ss}` : `${minutos}:${ss}`;
}

export function formatarDuracao(duration: string | null): string {
  if (!duration) return '';

  const [hours, minutes, seconds] = duration.split(':').map(Number);
  const total = hours * 3600 + minutes * 60 + Math.round(seconds);
  const horas = Math.floor(total / 3600);
  const minutos = Math.floor((total % 3600) / 60);
  const segundos = total % 60;

  const mm = String(minutos).padStart(2, '0');
  const ss = String(segundos).padStart(2, '0');

  return horas > 0 ? `${horas}:${mm}:${ss}` : `${minutos}:${ss}`;
}

const VOLUME_KEY = 'sonaris.player.volume';
const MUTE_KEY = 'sonaris.player.muted';

export function lerVolumeInicial(): number {
  try {
    const v = parseFloat(localStorage.getItem(VOLUME_KEY) ?? '');
    return Number.isFinite(v) && v >= 0 && v <= 1 ? v : 0.8;
  } catch {
    return 0.8;
  }
}

export function lerMudoInicial(): boolean {
  try {
    return localStorage.getItem(MUTE_KEY) === '1';
  } catch {
    return false;
  }
}

export function salvarVolume(volume: number): void {
  localStorage.setItem(VOLUME_KEY, String(volume));
}

export function salvarMudo(mudo: boolean): void {
  localStorage.setItem(MUTE_KEY, mudo ? '1' : '0');
}
