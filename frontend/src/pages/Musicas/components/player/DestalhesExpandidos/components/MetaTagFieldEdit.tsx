import type { ICamposEdicao } from "../../../../types";

interface IMetaTagFieldEdit {
    chave: keyof ICamposEdicao;
    campos: ICamposEdicao;
    rotulo: string;
    setCampos: React.Dispatch<React.SetStateAction<ICamposEdicao>>;
}
export function MetaTagFieldEdit({ campos, chave, rotulo, setCampos }: IMetaTagFieldEdit) {
    return (
        <label className="block">
            <span className="text-xs text-slate-400">{rotulo}</span>
            <input
                type="text"
                value={campos[chave]}
                onChange={(e) => setCampos((prev) => ({ ...prev, [chave]: e.target.value }))}
                className="mt-1 w-full bg-slate-50 dark:bg-slate-900 rounded-lg px-3 py-2 text-sm font-medium border border-slate-200 dark:border-slate-700 focus:outline-none focus:ring-2 focus:ring-blue-500"
            />
        </label>
    )
};