import { useEffect, useState } from 'react';

export type Page = 'cabinets' | 'upload' | 'export';

export const pages: { id: Page; label: string }[] = [
  { id: 'cabinets', label: 'File cabinet' },
  { id: 'upload', label: 'Upload' },
  { id: 'export', label: 'Export' }
];

export const homeHref = '#/cabinets';

export function pageHref(page: Page): string {
  return `#/${page}`;
}

export function useRoute(): Page {
  const [page, setPage] = useState(readPage);

  useEffect(() => {
    const onChange = () => setPage(readPage());
    window.addEventListener('hashchange', onChange);
    return () => window.removeEventListener('hashchange', onChange);
  }, []);

  return page;
}

function readPage(): Page {
  const id = window.location.hash.replace(/^#\/?/, '');
  return pages.find(page => page.id === id)?.id ?? 'cabinets';
}