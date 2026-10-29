import { useCallback, useEffect, useState } from 'react';
import { checkArchive, checkExport, type HealthStatus } from '../api/health';

export interface ServiceHealth {
  archive: HealthStatus;
  export: HealthStatus;
  checkedAt: Date | null;
}

export function useHealth(intervalSeconds: number) {
  const [health, setHealth] = useState<ServiceHealth>({ archive: 'checking', export: 'checking', checkedAt: null });

  const refresh = useCallback(async () => {
    const [archive, exportService] = await Promise.all([checkArchive(), checkExport()]);
    setHealth({ archive, export: exportService, checkedAt: new Date() });
  }, []);

  useEffect(() => {
    void refresh();
    if (intervalSeconds <= 0) {
      return;
    }

    const timer = setInterval(() => void refresh(), intervalSeconds * 1000);
    return () => clearInterval(timer);
  }, [intervalSeconds, refresh]);

  return { health, refresh };
}