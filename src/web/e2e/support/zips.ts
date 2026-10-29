import { readFile } from 'node:fs/promises';
import JSZip from 'jszip';

// Entry name -> content as base64; folder entries end with '/' and have ''.
export async function readZip(path: string): Promise<Record<string, string>> {
  const zip = await JSZip.loadAsync(await readFile(path), { createFolders: false });
  const entries: Record<string, string> = {};
  for (const entry of Object.values(zip.files)) {
    entries[entry.name] = entry.dir ? '' : await entry.async('base64');
  }

  return entries;
}
