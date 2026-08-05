import { ChevronRight, FolderTree } from "lucide-react";

interface IBreadcrumbMusicas {
    path: string;
    onNavigate: (path: string) => void;
}

export function BreadcrumbMusicas({ path, onNavigate }: IBreadcrumbMusicas) {
    const segments = path.split("/").filter(Boolean);

    return (
        <nav className="flex items-center gap-1 flex-wrap text-sm">
            <button
                onClick={() => onNavigate("")}
                className={`inline-flex items-center gap-1.5 px-2 py-1 rounded-md transition-colors font-medium ${path
                    ? "text-slate-400 hover:text-white hover:bg-slate-800"
                    : "text-blue-400"
                    }`}
            >
                <FolderTree size={14} />
                Raiz
            </button>

            {segments.map((segment, index) => {
                const segmentPath = segments.slice(0, index + 1).join("/");
                const isLast = index === segments.length - 1;

                return (
                    <span key={segmentPath} className="flex items-center gap-1">
                        <ChevronRight size={14} className="text-slate-600" />
                        <button
                            onClick={() => onNavigate(segmentPath)}
                            className={`px-2 py-1 rounded-md transition-colors font-medium ${isLast
                                ? "text-white cursor-default"
                                : "text-slate-400 hover:text-white hover:bg-slate-800"
                                }`}
                            disabled={isLast}
                        >
                            {segment}
                        </button>
                    </span>
                );
            })}
        </nav>
    );
}
