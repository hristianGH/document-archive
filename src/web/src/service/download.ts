import { exportDocuments } from '../api/export';
import type { ExportRequest } from '../api/types';

export async function exportAndSave(request: ExportRequest): Promise<string> {
  const file = await exportDocuments(request);
  const url = URL.createObjectURL(file.content);
  try {
    const link = document.createElement('a');
    link.href = url;
    link.download = file.name;
    link.click();
  } finally {
    setTimeout(() => URL.revokeObjectURL(url), 0);
  }

  return file.name;
}
