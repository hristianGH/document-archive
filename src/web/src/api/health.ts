import { config } from '../config';
import { joinUrl } from './http';

export type HealthStatus = 'checking' | 'up' | 'down';

const timeoutMs = 5000;

export function checkArchive(): Promise<HealthStatus> {
  return check(joinUrl(config().archiveUrl, '/health/ready'));
}

export function checkExport(): Promise<HealthStatus> {
  return check(joinUrl(config().exportUrl, '/health/live'));
}

async function check(url: string): Promise<HealthStatus> {
  try {
    const response = await fetch(url, { cache: 'no-store', signal: AbortSignal.timeout(timeoutMs) });
    return response.ok ? 'up' : 'down';
  } catch {
    return 'down';
  }
}