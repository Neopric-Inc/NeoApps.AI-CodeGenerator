# syntax=docker/dockerfile:1
# Build the React app (API URL is baked in at build time: .env, or the REACT_APP_API_BASE_URL build arg)
FROM node:22-alpine AS build
WORKDIR /app
# "/v1/api/" = same origin, proxied by nginx below (set by neoapps deploy); empty = use .env
ARG REACT_APP_API_BASE_URL
COPY package.json package-lock.json ./
ENV CYPRESS_INSTALL_BINARY=0
RUN npm install --force --no-audit --no-fund
COPY . .
ENV NODE_OPTIONS="--max-old-space-size=4096" GENERATE_SOURCEMAP=false CI=false
RUN if [ -n "$REACT_APP_API_BASE_URL" ]; then sed -i "s#^REACT_APP_API_BASE_URL=.*#REACT_APP_API_BASE_URL=$REACT_APP_API_BASE_URL#" .env; fi && npm run build

# Serve the static build
FROM nginx:alpine
# where /v1/api/ is proxied to (compose sets it to this app's API service)
ENV API_UPSTREAM=http://api:8080
COPY ./default.conf /etc/nginx/templates/default.conf.template
COPY --from=build /app/build /usr/share/nginx/html
EXPOSE 80
