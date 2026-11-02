# syntax=docker/dockerfile:1
# Build the React app (API URL is baked in from .env at build time)
FROM node:22-alpine AS build
WORKDIR /app
COPY package.json package-lock.json ./
ENV CYPRESS_INSTALL_BINARY=0
RUN npm install --force --no-audit --no-fund
COPY . .
ENV NODE_OPTIONS="--max-old-space-size=4096" GENERATE_SOURCEMAP=false CI=false
RUN npm run build

# Serve the static build
FROM nginx:alpine
COPY ./default.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/build /usr/share/nginx/html
EXPOSE 80
