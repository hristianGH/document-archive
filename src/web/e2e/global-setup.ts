import { archiveApiUrl, exportApiUrl, webUrl } from './support/stack';

const startTimeoutMs = 120_000;

export default async function globalSetup() {
  await waitFor(webUrl);
  await waitFor(`${archiveApiUrl}/health/ready`);
  await waitFor(`${exportApiUrl}/health/live`);
}

async function waitFor(url: string) {
  const deadline = Date.now() + startTimeoutMs;
  while (Date.now() < deadline) {
    const response = await fetch(url).catch(() => undefined);
    if (response?.ok) {
      return;
    }

    await new Promise(resolve => setTimeout(resolve, 1000));
  }

  throw new Error(`${url} did not answer within ${startTimeoutMs / 1000} seconds.`);
}