import { useState, type FormEvent } from 'react';
import { uploadFolder } from '../api/archive';
import type { Cabinet } from '../api/types';
import { Empty } from '../components/Empty';
import { ErrorMessage } from '../components/ErrorMessage';
import { Loading } from '../components/Loading';
import { writableTextFields } from '../service/fields';
import { useRequest } from '../service/useRequest';
import { UploadReportView } from './UploadReportView';

export function UploadView({ cabinet }: { cabinet: Cabinet }) {
  const fields = writableTextFields(cabinet);
  const [field, setField] = useState(fields[0]?.name ?? '');
  const [zip, setZip] = useState<File | null>(null);
  const { state, run } = useRequest(uploadFolder);
  const busy = state.status === 'loading';

  if (fields.length === 0) {
    return (
      <section>
        <h2>Upload</h2>
        <Empty>This cabinet has no writable text field to hold the folder name.</Empty>
      </section>
    );
  }

  const submit = (event: FormEvent) => {
    event.preventDefault();
    if (zip) {
      void run(cabinet.name, field, zip);
    }
  };

  return (
    <section>
      <h2>Upload</h2>
      <form onSubmit={submit}>
        <label>
          Field for the folder name{' '}
          <select value={field} onChange={event => setField(event.target.value)} disabled={busy}>
            {fields.map(f => (
              <option key={f.name} value={f.name}>
                {f.label}
              </option>
            ))}
          </select>
        </label>
        <label>
          Folder as a zip{' '}
          <input type="file" accept=".zip,application/zip" onChange={event => setZip(event.target.files?.[0] ?? null)} disabled={busy} />
        </label>
        <button type="submit" disabled={busy || !zip}>
          Upload
        </button>
      </form>

      {busy && <Loading label="Uploading…" />}
      {state.status === 'success' && <UploadReportView report={state.data} />}
      {state.status === 'error' && (
        <>
          <ErrorMessage problem={state.problem} />
          {state.problem.report && <UploadReportView report={state.problem.report} />}
        </>
      )}
    </section>
  );
}
