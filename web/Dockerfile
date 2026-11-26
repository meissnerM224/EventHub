# Proxy image: Caddy with the built React app and its Caddyfile.
# Build context is the repository root (it needs both web/ and the Caddyfile):
#   docker build -f web/Dockerfile .

FROM node:26-alpine AS build
# Same pnpm major as CI (.github/workflows/web.yaml). Corepack is no longer bundled with Node 25+.
RUN npm install --global pnpm@11
WORKDIR /web

# Dependencies first, so this layer is cached until the lockfile changes.
COPY web/package.json web/pnpm-lock.yaml ./
RUN pnpm install --frozen-lockfile

COPY web/ ./
RUN pnpm build

FROM caddy:2-alpine AS final
COPY Caddyfile /etc/caddy/Caddyfile
COPY --from=build /web/dist /srv/web
