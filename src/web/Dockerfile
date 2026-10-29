FROM node:24-alpine AS build
WORKDIR /web
COPY package.json package-lock.json ./
RUN npm ci
COPY . .
RUN npm run build

FROM nginx:stable-alpine
ENV ARCHIVE_URL=/api/archive \
    EXPORT_URL=/api/export \
    ARCHIVE_UPSTREAM=http://archive:8080 \
    EXPORT_UPSTREAM=http://export:8080 \
    NGINX_RESOLVER=127.0.0.11
COPY nginx/default.conf.template /etc/nginx/templates/default.conf.template
COPY --chmod=755 nginx/40-web-config.sh /docker-entrypoint.d/40-web-config.sh
COPY --from=build /web/dist /usr/share/nginx/html
EXPOSE 8080
