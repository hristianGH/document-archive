import type { Problem } from '../api/http';

export function ErrorMessage({ problem }: { problem: Problem }) {
  const fieldErrors = Object.entries(problem.errors ?? {});
  return (
    <div className="error" role="alert">
      <p>{problem.detail}</p>
      {fieldErrors.length > 0 && (
        <ul>
          {fieldErrors.flatMap(([field, messages]) =>
            messages.map(message => (
              <li key={`${field}:${message}`}>
                <strong>{field}:</strong> {message}
              </li>
            ))
          )}
        </ul>
      )}
      {problem.traceId && <p className="trace">Trace id: {problem.traceId}</p>}
    </div>
  );
}
