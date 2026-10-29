import { expect, type APIRequestContext } from '@playwright/test';
import { archiveApiUrl } from './stack';

interface DocumentPage {
  items: { indexValues: Record<string, unknown> }[];
  page: number;
  pageSize: number;
  totalCount: number;
}

// The UI does not show index values, so the marker is read from the Archive API.
export async function fieldValues(request: APIRequestContext, cabinet: string, day: string, field: string): Promise<unknown[]> {
  const values: unknown[] = [];
  for (let page = 1; ; page++) {
    const response = await request.get(`${archiveApiUrl}/cabinets/${cabinet}/documents`, { params: { from: day, to: day, page } });
    await expect(response).toBeOK();
    const body = (await response.json()) as DocumentPage;
    values.push(...body.items.map(item => item.indexValues[field]));
    if (body.items.length === 0 || page * body.pageSize >= body.totalCount) {
      return values;
    }
  }
}
