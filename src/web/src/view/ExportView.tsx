import { useState, type FormEvent } from 'react';
import type { Cabinet } from '../api/types';
import { Empty } from '../components/Empty';
import { ErrorMessage } from '../components/ErrorMessage';
import { Loading } from '../components/Loading';
import { exportAndSave } from '../service/download';
import { writableTextFields } from '../service/fields';
import { useRequest } from '../service/useRequest';

export function ExportView({ cabinet }: { cabinet: Cabinet }) {
  const fields = writableTextFields(cabinet);
  const [field, setField] = useState(fields[0]?.name ?? '');
  const [from, setFrom] = useState('');
  const [to, setTo] = useState('');
  const { state, run } = useRequest(exportAndSave);
  const busy = state.status === 'loading';

  if (fields.length === 0) {
    return (
      <section>
        <h2>Export</h2>
        <Empty>This cabinet has no writable text field to hold the export marker.</Empty>
      </section>
    );
  }

  const submit = (event: FormEvent) => {
    event.preventDefault();
    void run({ cabinet: cabinet.name, field, from, to });
  };

  return (
    <section>
      <h2>Export</h2>
      <form onSubmit={submit}>
        <label>
          Field for the export marker{' '}
          <select value={field} onChange={event => setField(event.target.value)} disabled={busy}>
            {fields.map(f => (
              <option key={f.name} value={f.name}>
                {f.label}
              </option>
            ))}
          </select>
        </label>
        <label>
          From{' '}
          <input type="date" value={from} onChange={event => setFrom(event.target.value)} disabled={busy} />
        </label>
        <label>
          To{' '}
          <input type="date" value={to} onChange={event => setTo(event.target.value)} disabled={busy} />
        </label>
        <button type="submit" disabled={busy || !from || !to}>
          Export
        </button>
      </form>
      <p className="hint">Dates are whole days in UTC, both included.</p>

      {busy && <Loading label="Exporting… the download starts when the archive is complete." />}
      {state.status === 'success' && <p className="success">Downloaded {state.data}.</p>}
      {state.status === 'error' && <ErrorMessage problem={state.problem} />}
    </section>
  );
}
