# syntax=docker/dockerfile:1
#
# Reverse proxy image: Caddy serving the production build of the Vue app. Build context: frontend/
# The Caddyfile is not baked in; deploy/compose.yaml mounts deploy/Caddyfile read-only.

# ---- build: Vue production build -------------------------------------------------------------------
# Major version as in .nvmrc.
FROM docker.io/library/node:24-alpine AS build
WORKDIR /app

COPY package.json package-lock.json ./
RUN npm ci

COPY . .
# Type-check, lint and tests run in CI; the image only needs the bundle.
RUN npm run build-only

# ---- proxy -----------------------------------------------------------------------------------------
FROM docker.io/library/caddy:2-alpine AS proxy
COPY --from=build /app/dist /srv
