# Production Nginx: builds the Vue SPA (node stage) and serves it,
# proxying API/OpenAPI/health traffic to the .NET backend.
FROM node:22-alpine AS webbuild
WORKDIR /app
COPY frontend/package.json frontend/package-lock.json* ./
RUN npm ci || npm install
COPY frontend/ ./
RUN npm run build

FROM nginx:1.27-alpine AS final
COPY nginx/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=webbuild /app/dist /usr/share/nginx/html
EXPOSE 80
