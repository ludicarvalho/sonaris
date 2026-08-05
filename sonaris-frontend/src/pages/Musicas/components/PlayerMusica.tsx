import { useEffect, useRef, useState } from "react";
import { Music2, Pause, Play, SkipBack, SkipForward, Volume1, Volume2, VolumeX, X } from "lucide-react";
import { getCapaMusica, getMusicaMetadata, streamUrl } from "../services/musicas.service";
import type { FileSystemItem, MusicMetadata } from "../types";
import { removerExensaoArquivo } from "../../../utils/text";
import { formatarTamanho } from "../utils";

interface IPlayerMusica {
    track: FileSystemItem;
    onClose: () => void;
    onPrev: () => void;
    onNext: () => void;
    hasPrev: boolean;
    hasNext: boolean;
}

function formatarTempo(seg: number): string {
    if (!seg || Number.isNaN(seg)) return "0:00";

    const horas = Math.floor(seg / 3600);
    const minutos = Math.floor((seg % 3600) / 60);
    const segundos = Math.floor(seg % 60);
    const ss = String(segundos).padStart(2, "0");

    return horas > 0 ? `${horas}:${String(minutos).padStart(2, "0")}:${ss}` : `${minutos}:${ss}`;
}

function formatarDuracao(duration: string | null): string {
    if (!duration) return "";

    const [hours, minutes, seconds] = duration.split(":").map(Number);
    const total = hours * 3600 + minutes * 60 + Math.round(seconds);
    const horas = Math.floor(total / 3600);
    const minutos = Math.floor((total % 3600) / 60);
    const segundos = total % 60;

    const mm = String(minutos).padStart(2, "0");
    const ss = String(segundos).padStart(2, "0");

    return horas > 0 ? `${horas}:${mm}:${ss}` : `${minutos}:${ss}`;
}

const VOLUME_KEY = 'sonaris.player.volume';
const MUTE_KEY = 'sonaris.player.muted';

function lerVolumeInicial(): number {
    try {
        const v = parseFloat(localStorage.getItem(VOLUME_KEY) ?? '');
        return Number.isFinite(v) && v >= 0 && v <= 1 ? v : 0.8;
    } catch {
        return 0.8;
    }
}

function lerMudoInicial(): boolean {
    try {
        return localStorage.getItem(MUTE_KEY) === '1';
    } catch {
        return false;
    }
}

export function PlayerMusica({ track, onClose, onPrev, onNext, hasPrev, hasNext }: IPlayerMusica) {
    const audioRef = useRef<HTMLAudioElement | null>(null);
    const [metadata, setMetadata] = useState<MusicMetadata | null>(null);
    const [capaUrl, setCapaUrl] = useState<string | null>(null);
    const [expandido, setExpandido] = useState(false);
    const [tocando, setTocando] = useState(false);
    const [tempoAtual, setTempoAtual] = useState(0);
    const [tempoTotal, setTempoTotal] = useState(0);
    const [progresso, setProgresso] = useState(0);
    const [buffer, setBuffer] = useState(0);
    const [volume, setVolume] = useState<number>(lerVolumeInicial);
    const [mudo, setMudo] = useState<boolean>(lerMudoInicial);

    const alternarPlayPause = () => {
        const audio = audioRef.current;
        if (!audio) return;
        if (audio.paused) audio.play().catch(() => { });
        else audio.pause();
    };

    const atualizarTempo = () => {
        const audio = audioRef.current;
        if (!audio) return;
        setTempoAtual(audio.currentTime);
        if (audio.duration) setProgresso((audio.currentTime / audio.duration) * 100);
    };

    const aoCarregarDuracao = () => {
        const audio = audioRef.current;
        if (!audio) return;
        setTempoTotal(audio.duration || 0);
    };

    const atualizarBuffer = () => {
        const audio = audioRef.current;
        if (!audio || !audio.duration || !audio.buffered.length) return;
        setBuffer((audio.buffered.end(audio.buffered.length - 1) / audio.duration) * 100);
    };

    const buscarPosicao = (e: React.MouseEvent<HTMLDivElement>) => {
        const audio = audioRef.current;
        if (!audio) return;
        const rect = e.currentTarget.getBoundingClientRect();
        const frac = Math.min(1, Math.max(0, (e.clientX - rect.left) / rect.width));
        audio.currentTime = frac * audio.duration;
    };

    const alterarVolume = (e: React.ChangeEvent<HTMLInputElement>) => {
        const audio = audioRef.current;
        const v = parseFloat(e.target.value);
        setVolume(v);
        setMudo(v === 0);
        localStorage.setItem(VOLUME_KEY, String(v));
        if (v === 0) localStorage.setItem(MUTE_KEY, '1');
        if (!audio) return;
        audio.volume = v;
        audio.muted = false;
    };

    const alternarMudo = () => {
        const audio = audioRef.current;
        if (!audio) return;
        audio.muted = !audio.muted;
        setMudo(audio.muted);
        localStorage.setItem(MUTE_KEY, audio.muted ? '1' : '0');
    };

    // Atalhos de teclado enquanto o tocador está aberto
    useEffect(() => {
        const aoTeclar = (e: KeyboardEvent) => {
            const audio = audioRef.current;
            if (!audio) return;
            if (e.code === 'Space') {
                e.preventDefault();
                if (audio.paused) audio.play().catch(() => { });
                else audio.pause();
            }
            if (e.key === 'ArrowRight') {
                e.preventDefault();
                audio.currentTime += 5;
            }
            if (e.key === 'ArrowLeft') {
                e.preventDefault();
                audio.currentTime -= 5;
            }
            if (e.key === 'ArrowUp') {
                e.preventDefault();
                audio.volume = Math.min(1, audio.volume + 0.1);
            }
            if (e.key === 'ArrowDown') {
                e.preventDefault();
                audio.volume = Math.max(0, audio.volume - 0.1);
            }
            if (e.key === 'm' || e.key === 'M') audio.muted = !audio.muted;
        };

        window.addEventListener('keydown', aoTeclar);
        return () => window.removeEventListener('keydown', aoTeclar);
    }, []);

    // Mantém o mesmo elemento <audio> (volume persiste) e só troca a fonte
    useEffect(() => {
        const audio = audioRef.current;
        if (!audio) return;

        audio.src = streamUrl(track.RelativePath);
        audio.volume = lerVolumeInicial();
        audio.muted = lerMudoInicial();
        audio.play().catch(() => { });
    }, [track]);

    // Busca os metadados (título, artista, álbum, duração, bitrate, etc.)
    useEffect(() => {
        let active = true;
        setMetadata(null);

        getMusicaMetadata(track.RelativePath)
            .then((res) => {
                if (!active) return;
                const { Success, Data } = res.data;
                if (Success && Data) setMetadata(Data);
            })
            .catch(() => { });

        return () => {
            active = false;
        };
    }, [track]);

    // Busca a capa (fallback para o ícone quando a API devolve erro)
    useEffect(() => {
        let active = true;
        let objectUrl: string | null = null;
        setCapaUrl(null);

        getCapaMusica(track.RelativePath)
            .then((res) => {
                if (!active) return;
                objectUrl = URL.createObjectURL(res.data);
                setCapaUrl(objectUrl);
            })
            .catch(() => {
                if (active) setCapaUrl(null);
            });

        return () => {
            active = false;
            if (objectUrl) URL.revokeObjectURL(objectUrl);
        };
    }, [track]);

    const titulo = metadata?.Title || removerExensaoArquivo(track.Name);
    const artistaAlbum = [metadata?.Artist, metadata?.Album].filter(Boolean).join(" • ");
    const capaPequena = capaUrl
        ? <img src={capaUrl} alt="" className="w-9 h-9 rounded-lg object-cover shrink-0" />
        : <div className="flex items-center justify-center w-9 h-9 rounded-lg bg-blue-600 text-white shrink-0">
            <Music2 size={18} />
        </div>;

    const capaGrande = capaUrl
        ? <img src={capaUrl} alt="" className="w-full sm:w-56 h-56 rounded-xl object-cover shadow-lg shrink-0" />
        : <div className="w-full sm:w-56 h-56 rounded-xl bg-gradient-to-br from-blue-600 to-indigo-700 flex items-center justify-center text-white shrink-0">
            <Music2 size={40} />
        </div>;

    const btnControle = "flex items-center justify-center w-8 h-8 rounded-full text-slate-500 dark:text-slate-400 hover:text-slate-800 dark:hover:text-white hover:bg-slate-200 dark:hover:bg-slate-700 transition-colors shrink-0 disabled:opacity-40 disabled:hover:text-slate-500 disabled:dark:hover:text-slate-400 disabled:hover:bg-transparent disabled:dark:hover:bg-transparent disabled:cursor-not-allowed";
    const btnExtra = "flex items-center justify-center w-8 h-8 rounded-lg text-slate-400 dark:text-slate-500 hover:text-slate-800 dark:hover:text-white hover:bg-slate-100 dark:hover:bg-slate-700 transition-colors shrink-0";

    return (
        <div className="fixed bottom-0 left-0 right-0 z-50 bg-white dark:bg-slate-800 border-t border-slate-200 dark:border-slate-700 shadow-[0_-8px_24px_rgba(0,0,0,0.08)]">
            {expandido && (
                <div className="border-b border-slate-200 dark:border-slate-700">
                    <div className="max-w-4xl mx-auto px-4 py-6">
                        <div className="flex flex-col sm:flex-row gap-6 sm:items-start">
                            {capaGrande}
                            <div className="min-w-0 flex-1">
                                <p className="text-xs text-blue-500 font-semibold uppercase tracking-wide">Agora tocando</p>
                                <h2 className="text-2xl font-bold mt-1 truncate">{titulo}</h2>
                                <p className="text-slate-400 dark:text-slate-500 truncate">{artistaAlbum}</p>

                                <dl className="grid grid-cols-2 sm:grid-cols-3 gap-3 mt-5 text-sm">
                                    <div className="bg-slate-50 dark:bg-slate-900 rounded-lg p-3">
                                        <dt className="text-xs text-slate-400">Álbum</dt>
                                        <dd className="font-medium truncate" title={metadata?.Album ?? ''}>{metadata?.Album || '—'}</dd>
                                    </div>
                                    <div className="bg-slate-50 dark:bg-slate-900 rounded-lg p-3">
                                        <dt className="text-xs text-slate-400">Faixa</dt>
                                        <dd className="font-medium">{metadata?.Track || '—'}</dd>
                                    </div>
                                    <div className="bg-slate-50 dark:bg-slate-900 rounded-lg p-3">
                                        <dt className="text-xs text-slate-400">Ano</dt>
                                        <dd className="font-medium">{metadata?.Year || '—'}</dd>
                                    </div>
                                    <div className="bg-slate-50 dark:bg-slate-900 rounded-lg p-3">
                                        <dt className="text-xs text-slate-400">Duração</dt>
                                        <dd className="font-medium">{formatarDuracao(metadata?.Duration ?? null) || '—'}</dd>
                                    </div>
                                    <div className="bg-slate-50 dark:bg-slate-900 rounded-lg p-3">
                                        <dt className="text-xs text-slate-400">Bitrate</dt>
                                        <dd className="font-medium">{metadata?.Bitrate ? `${metadata.Bitrate} kbps` : '—'}</dd>
                                    </div>
                                    <div className="bg-slate-50 dark:bg-slate-900 rounded-lg p-3">
                                        <dt className="text-xs text-slate-400">Tamanho</dt>
                                        <dd className="font-medium">{formatarTamanho(track.Size)}</dd>
                                    </div>
                                </dl>
                            </div>
                        </div>
                    </div>
                </div>
            )}

            <div className="px-4 py-3 flex items-center gap-4">
                {/* Bloco esquerdo: capa + dados da música (colado à esquerda, clique expande/recolhe) */}
                <div
                    onClick={() => setExpandido(v => !v)}
                    title={expandido ? "Recolher detalhes" : "Expandir detalhes"}
                    className="flex items-center gap-3 min-w-0 cursor-pointer select-none"
                >
                    {capaPequena}
                    <div className="min-w-0">
                        <p className="text-sm font-semibold text-slate-800 dark:text-slate-100 truncate">{titulo}</p>
                        <p className="text-xs text-slate-500 dark:text-slate-400 truncate">
                            {artistaAlbum || track.RelativePath}
                        </p>
                    </div>
                </div>

                {/* Bloco central: controles (ocupa o meio) */}
                <div className="flex-1 flex flex-col items-center gap-1 min-w-0">
                    <div className="flex items-center gap-2">
                        <button
                            onClick={onPrev}
                            disabled={!hasPrev}
                            title="Faixa anterior"
                            className={btnControle}
                        >
                            <SkipBack size={18} />
                        </button>

                        <button
                            onClick={alternarPlayPause}
                            title={tocando ? "Pausar" : "Reproduzir"}
                            className="flex items-center justify-center w-11 h-11 rounded-full bg-blue-600 hover:bg-blue-700 text-white transition-colors shrink-0"
                        >
                            {tocando ? <Pause size={20} /> : <Play size={20} className="ml-0.5" />}
                        </button>

                        <button
                            onClick={onNext}
                            disabled={!hasNext}
                            title="Próxima faixa"
                            className={btnControle}
                        >
                            <SkipForward size={18} />
                        </button>
                    </div>

                    <div className="flex items-center gap-2 w-full max-w-xl">
                        <span className="text-[11px] text-slate-500 dark:text-slate-400 tabular-nums shrink-0">
                            {formatarTempo(tempoAtual)}
                        </span>

                        <div
                            onClick={buscarPosicao}
                            title="Buscar posição"
                            className="relative flex-1 h-1.5 bg-slate-200 dark:bg-slate-700 rounded-full cursor-pointer"
                        >
                            <div
                                className="absolute inset-y-0 left-0 rounded-full bg-slate-400 dark:bg-slate-500"
                                style={{ width: `${buffer}%` }}
                            />
                            <div
                                className="absolute inset-y-0 left-0 rounded-full bg-blue-600"
                                style={{ width: `${progresso}%` }}
                            />
                            <div
                                className="absolute top-1/2 -translate-y-1/2 w-3 h-3 rounded-full bg-blue-600 border-2 border-white dark:border-slate-800 shadow"
                                style={{ left: `calc(${progresso}% - 6px)` }}
                            />
                        </div>

                        <span className="text-[11px] text-slate-500 dark:text-slate-400 tabular-nums shrink-0">
                            {formatarTempo(tempoTotal)}
                        </span>
                    </div>
                </div>

                {/* Bloco direito: volume + extras (colado à direita) */}
                <div className="flex items-center justify-end gap-1 sm:gap-2 shrink-0">
                    <button
                        onClick={alternarMudo}
                        title={mudo || volume === 0 ? "Ativar som" : "Mudo"}
                        className={btnExtra}
                    >
                        {mudo || volume === 0 ? <VolumeX size={18} /> : volume < 0.5 ? <Volume1 size={18} /> : <Volume2 size={18} />}
                    </button>
                    <input
                        type="range"
                        min="0"
                        max="1"
                        step="0.01"
                        value={mudo ? 0 : volume}
                        onChange={alterarVolume}
                        title="Volume"
                        className="w-20 sm:w-24 accent-blue-600 cursor-pointer hidden sm:block"
                    />
                    <button
                        onClick={onClose}
                        title="Fechar tocador"
                        className={btnExtra}
                    >
                        <X size={18} />
                    </button>
                </div>
            </div>

            <audio
                ref={audioRef}
                autoPlay
                onPlay={() => setTocando(true)}
                onPause={() => setTocando(false)}
                onEnded={() => {
                    setTocando(false);
                    if (hasNext) onNext();
                }}
                onTimeUpdate={atualizarTempo}
                onLoadedMetadata={aoCarregarDuracao}
                onProgress={atualizarBuffer}
                onVolumeChange={() => {
                    const audio = audioRef.current;
                    if (!audio) return;
                    setVolume(audio.volume);
                    setMudo(audio.muted);
                    localStorage.setItem(VOLUME_KEY, String(audio.volume));
                    localStorage.setItem(MUTE_KEY, audio.muted ? '1' : '0');
                }}
                className="hidden"
            />
        </div>
    );
}
