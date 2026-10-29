export function Endpoint({ method, path }: { method: string; path: string }) {
  return (
    <code className="endpoint">
      <span className="endpoint-method">{method}</span> {path}
    </code>
  );
}