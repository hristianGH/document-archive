import { useId, type ReactNode } from 'react';

interface PanelProps {
  title: string;
  aside?: ReactNode;
  children: ReactNode;
}

export function Panel({ title, aside, children }: PanelProps) {
  const id = useId();
  return (
    <section className="panel" aria-labelledby={id}>
      <header className="panel-head">
        <h2 id={id} className="panel-title">
          {title}
        </h2>
        {aside}
      </header>
      <div className="panel-body">{children}</div>
    </section>
  );
}