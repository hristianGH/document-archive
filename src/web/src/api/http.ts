import type { UploadReport } from './types';

export interface Problem {
  status: number;
  detail: string;
  errors?: Record<string, string[]>;
  traceId?: string;
  report?: UploadReport;
}

export class ApiError extends Error {
  readonly problem: Problem;

  constructor(problem: Problem) {
    super(problem.detail);
    this.problem = problem;
  }
}

export function joinUrl(baseUrl: string, path: string): string {
  return `${baseUrl.replace(/\/+$/, '')}${path}`;
}

export async function send(url: string, init?: RequestInit): Promise<Response> {
  let response: Response;
  try {
    response = await fetch(url, init);
  } catch {
    throw new ApiError({ status: 0, detail: 'The service could not be reached. Check that it is running and try again.' });
  }

  if (!response.ok) {
    throw new ApiError(await readProblem(response));
  }

  return response;
}

export function toProblem(error: unknown): Problem {
  if (error instanceof ApiError) {
    return error.problem;
  }

  return { status: 0, detail: error instanceof Error ? error.message : String(error) };
}

export function summarize(problem: Problem): string {
  return Object.values(problem.errors ?? {}).flat()[0] ?? problem.detail;
}

async function readProblem(response: Response): Promise<Problem> {
  const fallback = `The request failed with status ${response.status}${response.statusText ? ` (${response.statusText})` : ''}.`;
  if (!(response.headers.get('Content-Type') ?? '').includes('json')) {
    return { status: response.status, detail: fallback };
  }

  try {
    const body = await response.json();
    return {
      status: response.status,
      detail: body.detail ?? body.title ?? fallback,
      errors: body.errors,
      traceId: body.traceId,
      report: body.report
    };
  } catch {
    return { status: response.status, detail: fallback };
  }
}
