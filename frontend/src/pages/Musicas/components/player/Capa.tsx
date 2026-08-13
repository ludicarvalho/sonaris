import { Music2 } from "lucide-react";

interface ICapa {
    capaUrl: string | null;
    grande?: boolean;
}

export function Capa({ capaUrl, grande = false }: ICapa) {
    if (capaUrl) {
        return grande
            ? <img src={capaUrl} alt="" className="w-full sm:w-56 aspect-square rounded-xl object-cover shadow-lg shrink-0" />
            : <img src={capaUrl} alt="" className="w-9 h-9 rounded-lg object-cover shrink-0" />;
    }

    return grande
        ? <div className="w-full sm:w-56 aspect-square rounded-xl bg-gradient-to-br from-blue-600 to-indigo-700 flex items-center justify-center text-white shrink-0">
            <Music2 size={40} />
        </div>
        : <div className="flex items-center justify-center w-9 h-9 rounded-lg bg-blue-600 text-white shrink-0">
            <Music2 size={18} />
        </div>;
}
