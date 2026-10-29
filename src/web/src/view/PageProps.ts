import type { Cabinet } from '../api/types';

export interface PageProps {
  cabinets: Cabinet[];
  cabinet: Cabinet;
  onSelectCabinet: (name: string) => void;
}