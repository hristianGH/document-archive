import { useState, type FormEvent } from 'react';
import { Empty } from '../components/Empty';
import { Endpoint } from '../components/Endpoint';
import { PageHead } from '../components/PageHead';
import { Panel } from '../components/Panel';
import { ResponsePanel } from '../components/ResponsePanel';
import { SelectField } from '../components/SelectField';
import { exportAndSave } from '../service/download';
import { cabinetOptions, fieldOptions, writableTextFields } from '../service/fields';
import { useRequest } from '../service/useRequest';
import type { PageProps } from './PageProps';

const markerLength = 'Exported on yyyy-MM-dd'.length;

export function ExportView({ cabinets, cabinet, onSelectCabinet }: PageProps) {
  const fields = writableTextFields(cabinet);
  const [field, setField] = useState(fields[0]?.name ?? '');
  const [from, setFrom] = useState('');
  const [to, setTo] = useState('');
  const { state, run } = useRequest(exportAndSave, 'Export failed');
  const busy = state.status === 'loading';

  const submit = (event: FormEvent) => {
    event.preventDefault();
    void run({ cabinet: cabinet.name, field, from, to });
  };

  return (
    <>
      <PageHead title="Export documents">
        Download the documents archived between two dates as one zip. Each exported document is marked “Exported on” the export date in the field you pick.
      </PageHead>
      <div className="workspace">
        <Panel title="Request" aside={<Endpoint method="POST" path="/exports" />}>
          <form className="form" onSubmit={submit}>
            <SelectField label="Cabinet" value={cabinet.name} options={cabinetOptions(cabinets)} onChange={onSelectCabinet} disabled={busy} />
            {fields.length === 0 ? (
              <Empty>This cabinet has no writable text field to hold the export marker.</Empty>
            ) : (
              <>
                <SelectField
                  label="Field for the export marker"
                  value={field}
                  options={fieldOptions(fields)}
                  onChange={setField}
                  disabled={busy}
                  hint={`The marker needs a field that holds at least ${markerLength} characters.`}
                />
                <div className="field-row">
                  <label className="field">
                    <span className="field-label">From</span>
                    <input className="control" type="date" value={from} onChange={event => setFrom(event.target.value)} disabled={busy} />
                  </label>
                  <label className="field">
                    <span className="field-label">To</span>
                    <input className="control" type="date" value={to} onChange={event => setTo(event.target.value)} disabled={busy} />
                  </label>
                </div>
                <p className="field-hint">Dates are whole days in UTC, both included.</p>
                <div className="form-actions">
                  <button type="submit" className="button" disabled={busy || !from || !to}>
                    {busy ? 'Exporting…' : 'Export'}
                  </button>
                </div>
              </>
            )}
          </form>
        </Panel>
        <ResponsePanel
          state={state}
          idle="Nothing exported yet. Pick a field and a date range, then press Export."
          loading="Exporting… the download starts when the archive is complete."
        >
          {name => (
            <p className="callout callout-success">
              Downloaded <strong>{name}</strong>.
            </p>
          )}
        </ResponsePanel>
      </div>
    </>
  );
}