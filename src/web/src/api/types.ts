export type FieldType = 'text' | 'number' | 'date';

export interface IndexField {
  name: string;
  label: string;
  type: FieldType;
  maxLength: number | null;
  writable: boolean;
}

export interface Cabinet {
  name: string;
  displayName: string;
  fields: IndexField[];
}

export interface StoredDocument {
  documentId: string;
  title: string;
  partCount: number;
}

export interface UploadEntry {
  path: string;
  reason: string;
}

export interface UploadReport {
  folderName: string;
  field: string;
  message: string | null;
  stored: StoredDocument[];
  skipped: UploadEntry[];
  failed: UploadEntry[];
}

export interface ExportRequest {
  cabinet: string;
  field: string;
  from: string;
  to: string;
}

export interface ExportFile {
  name: string;
  content: Blob;
}
