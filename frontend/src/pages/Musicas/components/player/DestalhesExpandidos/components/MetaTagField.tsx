interface IMetaTagField
{
    label: string;
    value: string;
    title?: string
}

export function MetaTagField({ label, value, title }: IMetaTagField) {
    return (
        <div className="bg-slate-50 dark:bg-slate-900 rounded-lg p-3">
            <dt className="text-xs text-slate-400">{label}</dt>
            <dd className="font-medium truncate" title={title}>{value}</dd>
        </div>
    );
}