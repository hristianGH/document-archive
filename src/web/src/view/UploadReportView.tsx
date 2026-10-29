import type { UploadEntry, UploadReport } from '../api/types';

export function UploadReportView({ report }: { report: UploadReport }) {
  return (
    <div className="report">
      <p>
        Every stored document has <code>{report.field}</code> set to <strong>{report.folderName}</strong>.
      </p>
      {report.message && <p>{report.message}</p>}

      <section className="report-section">
        <h3 className="report-title">
          Stored <span className="count">{report.stored.length}</span>
        </h3>
        {report.stored.length === 0 ? (
          <p className="empty">None</p>
        ) : (
          <ul className="report-list">
            {report.stored.map(document => (
              <li key={document.documentId}>
                {document.title} <span className="muted">· {document.partCount} {document.partCount === 1 ? 'part' : 'parts'}</span>
              </li>
            ))}
          </ul>
        )}
      </section>

      <EntryList title="Skipped" entries={report.skipped} />
      <EntryList title="Failed" entries={report.failed} />
    </div>
  );
}

function EntryList({ title, entries }: { title: string; entries: UploadEntry[] }) {
  return (
    <section className="report-section">
      <h3 className="report-title">
        {title} <span className="count">{entries.length}</span>
      </h3>
      {entries.length === 0 ? (
        <p className="empty">None</p>
      ) : (
        <ul className="report-list">
          {entries.map(entry => (
            <li key={entry.path}>
              <code>{entry.path}</code> <span className="muted">· {entry.reason}</span>
            </li>
          ))}
        </ul>
      )}
    </section>
  );
}