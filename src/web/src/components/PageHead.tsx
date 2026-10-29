import type { ReactNode } from 'react';

export function PageHead({ title, children }: { title: string; children: ReactNode }) {
  return (
    <header className="page-head">
      <h1 className="page-title">{title}</h1>
      <p className="page-intro">{children}</p>
    </header>
  );
}