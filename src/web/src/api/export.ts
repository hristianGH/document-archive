import { config } from '../config';
import { ApiError, joinUrl, send } from './http';
import type { ExportFile, ExportRequest } from './types';

const fallbackFileName = 'export.zip';

export async function exportDocuments(request: ExportRequest): Promise<ExportFile> {
  const response = await send(joinUrl(config().exportUrl, '/exports'), {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(request)
  });

  const name = fileNameFrom(response.headers.get('Content-Disposition')) ?? fallbackFileName;
  try {
    return { name, content: await response.blob() };
  } catch {
    throw new ApiError({
      status: response.status,
      detail: 'The export broke off after the download started, so the archive is incomplete. Some documents may already carry the export marker. Try the export again.'
    });
  }
}

export function fileNameFrom(contentDisposition: string | null): string | undefined {
  if (!contentDisposition) {
    return undefined;
  }

  const encoded = /filename\*\s*=\s*UTF-8''([^;]+)/i.exec(contentDisposition);
  if (encoded) {
    try {
      return decodeURIComponent(encoded[1].trim());
    } catch {
      return undefined;
    }
  }

  return /filename\s*=\s*"?([^";]+)"?/i.exec(contentDisposition)?.[1].trim();
}
