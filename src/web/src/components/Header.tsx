import { homeHref, pageHref, pages, type Page } from '../service/route';
import type { Settings } from '../service/settings';
import type { ServiceHealth } from '../service/useHealth';
import { HealthIndicator } from './HealthIndicator';
import { SettingsMenu } from './SettingsMenu';

interface HeaderProps {
  page: Page;
  health: ServiceHealth;
  onCheckHealth: () => Promise<void>;
  settings: Settings;
  onSettingsChange: (change: Partial<Settings>) => void;
}

export function Header({ page, health, onCheckHealth, settings, onSettingsChange }: HeaderProps) {
  return (
    <header className="app-header">
      <div className="app-bar">
        <HealthIndicator health={health} />
        <a className="brand" href={homeHref}>
          Document Archive
        </a>
        <SettingsMenu settings={settings} onChange={onSettingsChange} onCheckHealth={onCheckHealth} />
      </div>
      <nav className="app-nav" aria-label="Pages">
        {pages.map(item => (
          <a key={item.id} className="app-nav-link" href={pageHref(item.id)} aria-current={item.id === page ? 'page' : undefined}>
            {item.label}
          </a>
        ))}
      </nav>
    </header>
  );
}