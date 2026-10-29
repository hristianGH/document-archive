export function Loading({ label }: { label: string }) {
  return (
    <p className="loading" role="status">
      <span className="loading-dot" aria-hidden="true" />
      {label}
    </p>
  );
}