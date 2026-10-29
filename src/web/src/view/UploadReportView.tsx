import type { UploadEntry, UploadReport } from '../api/types';

export function UploadReportView({ report }: { report: UploadReport }) {
  return (
    <div className="report">
      <p>
        Every stored document has <code>{report.field}</code> set to <strong>{report.folderName}</strong>.
      </p>
      {report.message && <p>{report.message}</p>}

      <h3>Stored ({report.stored.length})</h3>
      {report.stored.length === 0 ? (
        <p className="empty">None</p>
      ) : (
        <ul>
          {report.stored.map(document => (
            <li key={document.documentId}>
              {document.title} — {document.partCount} {document.partCount === 1 ? 'part' : 'parts'}
            </li>
          ))}
        </ul>
      )}

      <EntryList title="Skipped" entries={report.skipped} />
      <EntryList title="Failed" entries={report.failed} />
    </div>
  );
}

function EntryList({ title, entries }: { title: string; entries: UploadEntry[] }) {
  return (
    <>
      <h3>
        {title} ({entries.length})
      </h3>
      {entries.length === 0 ? (
        <p className="empty">None</p>
      ) : (
        <ul>
          {entries.map(entry => (
            <li key={entry.path}>
              <code>{entry.path}</code>: {entry.reason}
            </li>
          ))}
        </ul>
      )}
    </>
  );
}
