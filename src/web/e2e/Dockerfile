FROM mcr.microsoft.com/playwright:v1.64.1-noble
WORKDIR /web
COPY package.json package-lock.json ./
RUN npm ci
COPY playwright.config.ts ./
COPY e2e ./e2e
CMD ["npx", "playwright", "test"]