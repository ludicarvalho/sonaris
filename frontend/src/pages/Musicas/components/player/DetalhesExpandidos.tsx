import { useState } from "react";
import { Loader2, Pencil, Save, X } from "lucide-react";
import { Capa } from "./Capa";
import { formatarDuracao, formatarTamanho } from "../../utils";
import { AddToPlaylistButton } from "../AddToPlaylistButton";
import type { EditarMetadadosParams } from "../../services/musicas.service";
import type { FileSystemItem, MusicMetadata } from "../../types";

interface IDetalhesExpandidos {
    capaUrl: string | null;
    titulo: string;
    artistaAlbum: string;
    metadata: MusicMetadata | null;
    track: FileSystemItem;
    onSalvarMetadados: (params: EditarMetadadosParams) => Promise<void>;
}

interface ICamposEdicao {
    titulo: string;
    artista: string;
    album: string;
    faixa: string;
    ano: string;
}

function MetaCard({ label, value, title }: { label: string; value: string; title?: string }) {
    return (
        <div className="bg-slate-50 dark:bg-slate-900 rounded-lg p-3">
            <dt className="text-xs text-slate-400">{label}</dt>
            <dd className="font-medium truncate" title={title}>{value}</dd>
        </div>
    );
}

export function DetalhesExpandidos({ capaUrl, titulo, artistaAlbum, metadata, track, onSalvarMetadados }: IDetalhesExpandidos) {
    const [editando, setEditando] = useState(false);
    const [campos, setCampos] = useState<ICamposEdicao>({ titulo: "", artista: "", album: "", faixa: "", ano: "" });
    const [novaCapa, setNovaCapa] = useState<File | null>(null);
    const [removerCapa, setRemoverCapa] = useState(false);
    const [salvando, setSalvando] = useState(false);
    const [erro, setErro] = useState("");

    const entrarEmEdicao = () => {
        setCampos({
            titulo: metadata?.Title || "",
            artista: metadata?.Artist || "",
            album: metadata?.Album || "",
            faixa: metadata?.Track || "",
            ano: metadata?.Year || "",
        });
        setNovaCapa(null);
        setRemoverCapa(false);
        setErro("");
        setEditando(true);
    };

    const salvar = async (e: React.FormEvent) => {
        e.preventDefault();
        setSalvando(true);
        setErro("");

        try {
            await onSalvarMetadados({
                fileName: track.RelativePath,
                title: campos.titulo,
                artist: campos.artista,
                album: campos.album,
                track: campos.faixa,
                year: campos.ano,
                removerCapa: removerCapa && !novaCapa,
                capa: novaCapa,
            });
            setEditando(false);
        } catch (err: any) {
            setErro(err?.response?.data?.Message ?? err?.message ?? "Não foi possível salvar os metadados.");
        } finally {
            setSalvando(false);
        }
    };

    const campo = (chave: keyof ICamposEdicao, rotulo: string) => (
        <label className="block">
            <span className="text-xs text-slate-400">{rotulo}</span>
            <input
                type="text"
                value={campos[chave]}
                onChange={(e) => setCampos((prev) => ({ ...prev, [chave]: e.target.value }))}
                className="mt-1 w-full bg-slate-50 dark:bg-slate-900 rounded-lg px-3 py-2 text-sm font-medium border border-slate-200 dark:border-slate-700 focus:outline-none focus:ring-2 focus:ring-blue-500"
            />
        </label>
    );

    const inputClasse = "block w-full text-xs text-slate-500 dark:text-slate-400 file:mr-3 file:rounded-lg file:border-0 file:bg-blue-600 file:px-3 file:py-1.5 file:text-xs file:font-semibold file:text-white hover:file:bg-blue-700 cursor-pointer";

    return (
        <div className="border-b border-slate-200 dark:border-slate-700 flex-1 min-h-0 overflow-y-auto">
            <div className="max-w-4xl mx-auto px-4 py-6">
                <div className="flex flex-col sm:flex-row gap-6 sm:items-start">
                    <Capa capaUrl={capaUrl} grande />
                    <div className="min-w-0 flex-1">
                        <div className="flex items-start justify-between gap-3">
                            <div className="min-w-0">
                                <p className="text-xs text-blue-500 font-semibold uppercase tracking-wide">
                                    {editando ? "Editar metadados" : "Agora tocando"}
                                </p>
                                <h2 className="text-2xl font-bold mt-1 truncate">{editando ? track.Name : titulo}</h2>
                                <p className="text-slate-400 dark:text-slate-500 truncate">
                                    {editando ? track.RelativePath : artistaAlbum}
                                </p>
                            </div>
                            {!editando && (
                                <div className="flex items-center gap-2 shrink-0">
                                    <AddToPlaylistButton relativePath={track.RelativePath} />
                                    <button
                                        type="button"
                                        onClick={entrarEmEdicao}
                                        title="Editar metadados"
                                        className="inline-flex items-center gap-1.5 rounded-lg bg-slate-200/70 dark:bg-slate-700 px-3 py-1.5 text-xs font-semibold text-slate-700 dark:text-slate-100 hover:bg-slate-200 dark:hover:bg-slate-600 transition-colors"
                                    >
                                        <Pencil size={14} />
                                        Editar
                                    </button>
                                </div>
                            )}
                        </div>

                        {editando ? (
                            <form onSubmit={salvar} className="mt-5 space-y-4">
                                <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
                                    <div className="sm:col-span-2">{campo("titulo", "Título")}</div>
                                    {campo("artista", "Artista")}
                                    {campo("album", "Álbum")}
                                    {campo("faixa", "Faixa")}
                                    {campo("ano", "Ano")}
                                </div>

                                <div className="rounded-lg bg-slate-50 dark:bg-slate-900 p-3 space-y-2">
                                    <p className="text-xs text-slate-500 dark:text-slate-400">Capa</p>
                                    <input
                                        type="file"
                                        accept="image/*"
                                        onChange={(e) => {
                                            const arquivo = e.target.files?.[0] ?? null;
                                            setNovaCapa(arquivo);
                                            if (arquivo) setRemoverCapa(false);
                                        }}
                                        className={inputClasse}
                                    />
                                    {novaCapa && (
                                        <p className="text-xs text-blue-500 truncate">Nova capa: {novaCapa.name}</p>
                                    )}
                                    <label className="flex items-center gap-2 text-xs text-slate-600 dark:text-slate-300 cursor-pointer">
                                        <input
                                            type="checkbox"
                                            checked={removerCapa}
                                            onChange={(e) => {
                                                setRemoverCapa(e.target.checked);
                                                if (e.target.checked) setNovaCapa(null);
                                            }}
                                            className="accent-blue-600"
                                        />
                                        Remover capa embutida
                                    </label>
                                </div>

                                {erro && (
                                    <p className="text-sm text-red-600 dark:text-red-300 bg-red-500/10 border border-red-500/40 rounded-lg px-3 py-2">
                                        {erro}
                                    </p>
                                )}

                                <div className="flex items-center gap-2">
                                    <button
                                        type="submit"
                                        disabled={salvando}
                                        className="inline-flex items-center gap-1.5 rounded-lg bg-blue-600 px-4 py-2 text-sm font-semibold text-white hover:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
                                    >
                                        {salvando ? <Loader2 size={16} className="animate-spin" /> : <Save size={16} />}
                                        {salvando ? "Salvando..." : "Salvar"}
                                    </button>
                                    <button
                                        type="button"
                                        onClick={() => setEditando(false)}
                                        disabled={salvando}
                                        className="inline-flex items-center gap-1.5 rounded-lg bg-slate-200/70 dark:bg-slate-700 px-4 py-2 text-sm font-semibold text-slate-700 dark:text-slate-100 hover:bg-slate-200 dark:hover:bg-slate-600 disabled:opacity-50 transition-colors"
                                    >
                                        <X size={16} />
                                        Cancelar
                                    </button>
                                </div>
                            </form>
                        ) : (
                            <dl className="grid grid-cols-2 sm:grid-cols-3 gap-3 mt-5 text-sm">
                                <MetaCard label="Álbum" value={metadata?.Album || '—'} title={metadata?.Album ?? ''} />
                                <MetaCard label="Faixa" value={metadata?.Track || '—'} />
                                <MetaCard label="Ano" value={metadata?.Year || '—'} />
                                <MetaCard label="Duração" value={formatarDuracao(metadata?.Duration ?? null) || '—'} />
                                <MetaCard label="Bitrate" value={metadata?.Bitrate ? `${metadata.Bitrate} kbps` : '—'} />
                                <MetaCard label="Tamanho" value={formatarTamanho(track.Size)} />
                            </dl>
                        )}
                    </div>
                </div>
            </div>
        </div>
    );
}