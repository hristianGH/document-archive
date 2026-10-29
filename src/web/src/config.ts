export interface AppConfig {
  archiveUrl: string;
  exportUrl: string;
}

let current: AppConfig | undefined;

export async function loadConfig(): Promise<void> {
  const response = await fetch('/config.json', { cache: 'no-store' });
  if (!response.ok) {
    throw new Error(`The client settings (config.json) could not be loaded: status ${response.status}.`);
  }

  current = (await response.json()) as AppConfig;
}

export function config(): AppConfig {
  if (!current) {
    throw new Error('The client settings are not loaded.');
  }

  return current;
}
