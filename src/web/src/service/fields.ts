import type { Cabinet, IndexField } from '../api/types';

export function writableTextFields(cabinet: Cabinet): IndexField[] {
  return cabinet.fields.filter(field => field.type === 'text' && field.writable);
}

export function fieldOptions(fields: IndexField[]) {
  return fields.map(field => ({
    value: field.name,
    label: field.maxLength === null ? field.label : `${field.label} (up to ${field.maxLength} characters)`
  }));
}

export function cabinetOptions(cabinets: Cabinet[]) {
  return cabinets.map(cabinet => ({ value: cabinet.name, label: cabinet.displayName }));
}