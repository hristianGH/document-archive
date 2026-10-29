import type { HealthStatus } from '../api/health';
import type { ServiceHealth } from '../service/useHealth';

const spoken: Record<HealthStatus, string> = { checking: 'being checked', up: 'up', down: 'down' };

export function HealthIndicator({ health }: { health: ServiceHealth }) {
  const checked = health.checkedAt ? `Last checked at ${health.checkedAt.toLocaleTimeString()}` : 'Checking…';
  return (
    <ul className="health" aria-label="Service health" title={checked}>
      <HealthItem name="Archive" status={health.archive} />
      <HealthItem name="Export" status={health.export} />
    </ul>
  );
}

function HealthItem({ name, status }: { name: string; status: HealthStatus }) {
  return (
    <li className={`health-item health-${status}`}>
      <span className="health-dot" aria-hidden="true" />
      {name}
      <span className="visually-hidden"> is {spoken[status]}</span>
    </li>
  );
}