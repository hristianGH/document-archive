import { config } from '../config';
import { joinUrl, send } from './http';
import type { Cabinet, UploadReport } from './types';

function archiveUrl(path: string): string {
  return joinUrl(config().archiveUrl, path);
}

export async function getCabinets(): Promise<Cabinet[]> {
  const response = await send(archiveUrl('/cabinets'));
  return (await response.json()) as Cabinet[];
}

export async function uploadFolder(cabinet: string, field: string, zip: File): Promise<UploadReport> {
  const form = new FormData();
  form.append('field', field);
  form.append('file', zip);
  const response = await send(archiveUrl(`/cabinets/${encodeURIComponent(cabinet)}/uploads`), { method: 'POST', body: form });
  return (await response.json()) as UploadReport;
}
