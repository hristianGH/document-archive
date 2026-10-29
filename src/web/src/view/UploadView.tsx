import { useState, type FormEvent } from 'react';
import { uploadFolder } from '../api/archive';
import { Empty } from '../components/Empty';
import { Endpoint } from '../components/Endpoint';
import { PageHead } from '../components/PageHead';
import { Panel } from '../components/Panel';
import { ResponsePanel } from '../components/ResponsePanel';
import { SelectField } from '../components/SelectField';
import { cabinetOptions, fieldOptions, writableTextFields } from '../service/fields';
import { useRequest } from '../service/useRequest';
import type { PageProps } from './PageProps';
import { UploadReportView } from './UploadReportView';

export function UploadView({ cabinets, cabinet, onSelectCabinet }: PageProps) {
  const fields = writableTextFields(cabinet);
  const [field, setField] = useState(fields[0]?.name ?? '');
  const [zip, setZip] = useState<File | null>(null);
  const { state, run } = useRequest(uploadFolder, 'Upload failed');
  const busy = state.status === 'loading';

  const submit = (event: FormEvent) => {
    event.preventDefault();
    if (zip) {
      void run(cabinet.name, field, zip);
    }
  };

  return (
    <>
      <PageHead title="Upload a folder">
        Send one folder as a zip. Each file and each first-level subfolder becomes a document, and the folder name is written to the field you pick.
      </PageHead>
      <div className="workspace">
        <Panel title="Request" aside={<Endpoint method="POST" path={`/cabinets/${cabinet.name}/uploads`} />}>
          <form className="form" onSubmit={submit}>
            <SelectField label="Cabinet" value={cabinet.name} options={cabinetOptions(cabinets)} onChange={onSelectCabinet} disabled={busy} />
            {fields.length === 0 ? (
              <Empty>This cabinet has no writable text field to hold the folder name.</Empty>
            ) : (
              <>
                <SelectField label="Field for the folder name" value={field} options={fieldOptions(fields)} onChange={setField} disabled={busy} />
                <label className="field">
                  <span className="field-label">Folder as a zip</span>
                  <input
                    className="control control-file"
                    type="file"
                    accept=".zip,application/zip"
                    onChange={event => setZip(event.target.files?.[0] ?? null)}
                    disabled={busy}
                  />
                </label>
                <div className="form-actions">
                  <button type="submit" className="button" disabled={busy || !zip}>
                    {busy ? 'Uploading…' : 'Upload'}
                  </button>
                </div>
              </>
            )}
          </form>
        </Panel>
        <ResponsePanel
          state={state}
          idle="Nothing uploaded yet. Pick a field and a zip, then press Upload."
          loading="Uploading the folder…"
          error={problem => problem.report && <UploadReportView report={problem.report} />}
        >
          {report => <UploadReportView report={report} />}
        </ResponsePanel>
      </div>
    </>
  );
}