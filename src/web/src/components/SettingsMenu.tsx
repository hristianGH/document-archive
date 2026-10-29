import { useEffect, useRef, useState } from 'react';
import { config } from '../config';
import type { Settings } from '../service/settings';
import { GearIcon } from './Icons';
import { SelectField } from './SelectField';

const intervals = [
  { value: '0', label: 'Only when the page opens' },
  { value: '15', label: 'Every 15 seconds' },
  { value: '30', label: 'Every 30 seconds' },
  { value: '60', label: 'Every minute' }
];

interface SettingsMenuProps {
  settings: Settings;
  onChange: (change: Partial<Settings>) => void;
  onCheckHealth: () => Promise<void>;
}

export function SettingsMenu({ settings, onChange, onCheckHealth }: SettingsMenuProps) {
  const [open, setOpen] = useState(false);
  const root = useRef<HTMLDivElement>(null);
  const { archiveUrl, exportUrl } = config();

  useEffect(() => {
    if (!open) {
      return;
    }

    const onPointerDown = (event: PointerEvent) => {
      if (!root.current?.contains(event.target as Node)) {
        setOpen(false);
      }
    };
    const onKeyDown = (event: KeyboardEvent) => {
      if (event.key === 'Escape') {
        setOpen(false);
      }
    };
    document.addEventListener('pointerdown', onPointerDown);
    document.addEventListener('keydown', onKeyDown);
    return () => {
      document.removeEventListener('pointerdown', onPointerDown);
      document.removeEventListener('keydown', onKeyDown);
    };
  }, [open]);

  return (
    <div className="settings" ref={root}>
      <button
        type="button"
        className="icon-button"
        aria-label="Settings"
        aria-expanded={open}
        aria-controls="settings-panel"
        onClick={() => setOpen(current => !current)}
      >
        <GearIcon />
      </button>
      {open && (
        <div id="settings-panel" className="settings-panel" role="group" aria-label="Settings">
          <h2 className="settings-title">Settings</h2>
          <SelectField
            label="Health check"
            value={String(settings.healthIntervalSeconds)}
            options={intervals}
            onChange={value => onChange({ healthIntervalSeconds: Number(value) })}
          />
          <button type="button" className="button button-secondary" onClick={() => void onCheckHealth()}>
            Check now
          </button>
          <dl className="settings-services">
            <dt>Archive service</dt>
            <dd>
              <code>{archiveUrl}</code>
            </dd>
            <dt>Export service</dt>
            <dd>
              <code>{exportUrl}</code>
            </dd>
          </dl>
        </div>
      )}
    </div>
  );
}