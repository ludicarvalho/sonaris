import axios from 'axios';

interface MensagemErroApi {
    Message?: string | null;
    ErrorDetails?: string | null;
}

function ehMensagemApi(valor: unknown): valor is MensagemErroApi {
    return typeof valor === 'object' && valor !== null && ('Message' in valor || 'ErrorDetails' in valor);
}

export async function mensagemDeBlobErro(blob: Blob): Promise<string | null> {
    try {
        const texto = await blob.text();
        const parsed = JSON.parse(texto) as MensagemErroApi;
        return parsed.Message || parsed.ErrorDetails || null;
    } catch {
        return null;
    }
}

export async function erroParaMensagem(error: unknown): Promise<string> {
    if (axios.isAxiosError(error)) {
        const data = error.response?.data;

        if (data instanceof Blob) {
            const mensagem = await mensagemDeBlobErro(data);
            if (mensagem) return mensagem;
        } else if (ehMensagemApi(data)) {
            if (data.Message) return data.Message;
            if (data.ErrorDetails) return data.ErrorDetails;
        }

        if (!error.response) {
            return 'Não foi possível conectar ao servidor.';
        }

        return 'Não foi possível concluir a operação. Tente novamente.';
    }

    if (error instanceof Error && error.message) {
        return error.message;
    }

    return 'Ocorreu um erro inesperado. Tente novamente.';
}