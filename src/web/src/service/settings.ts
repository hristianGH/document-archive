import { useCallback, useEffect, useState } from 'react';

export interface Settings {
  healthIntervalSeconds: number;
}

const storageKey = 'document-archive.settings';
const defaults: Settings = { healthIntervalSeconds: 30 };

export function useSettings() {
  const [settings, setSettings] = useState<Settings>(load);

  useEffect(() => {
    localStorage.setItem(storageKey, JSON.stringify(settings));
  }, [settings]);

  const update = useCallback((change: Partial<Settings>) => setSettings(current => ({ ...current, ...change })), []);

  return { settings, update };
}

function load(): Settings {
  try {
    return { ...defaults, ...(JSON.parse(localStorage.getItem(storageKey) ?? '{}') as Partial<Settings>) };
  } catch {
    return defaults;
  }
}