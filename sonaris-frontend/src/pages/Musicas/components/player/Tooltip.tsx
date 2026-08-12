interface ITooltip {
    label: string;
    shortcut?: string;
    children: React.ReactNode;
    wrapperClassName?: string;
}

export function Tooltip({ label, shortcut, children, wrapperClassName = "" }: ITooltip) {
    return (
        <span className={`group relative inline-flex ${wrapperClassName}`}>
            {children}
            <span className="pointer-events-none absolute bottom-full left-1/2 -translate-x-1/2 mb-2 z-20 hidden group-hover:inline-flex items-center gap-1.5 whitespace-nowrap rounded-md bg-slate-900 dark:bg-slate-900 px-2 py-1 text-[11px] text-slate-50 shadow-lg">
                {label}
                {shortcut && (
                    <kbd className="rounded bg-white/15 px-1 py-px font-mono text-[10px] text-white leading-relaxed">
                        {shortcut}
                    </kbd>
                )}
            </span>
        </span>
    );
}