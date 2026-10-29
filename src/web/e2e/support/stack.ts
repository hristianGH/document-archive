import { fileURLToPath } from 'node:url';

// Inside the e2e container the web is http://web:8080; from the host it is published on 18080.
export const webUrl = process.env.E2E_BASE_URL ?? 'http://localhost:18080';
export const archiveApiUrl = `${webUrl}/api/archive`;
export const exportApiUrl = `${webUrl}/api/export`;

export function fixture(name: string): string {
  return fileURLToPath(new URL(`../fixtures/${name}`, import.meta.url));
}

export function todayUtc(): string {
  return new Date().toISOString().slice(0, 10);
}