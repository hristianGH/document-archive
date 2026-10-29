import type { Cabinet, IndexField } from '../api/types';

export function writableTextFields(cabinet: Cabinet): IndexField[] {
  return cabinet.fields.filter(field => field.type === 'text' && field.writable);
}
