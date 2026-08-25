# Stage 1: build the React app
FROM node:20-alpine AS build
WORKDIR /app

COPY Frontend/package*.json ./
RUN npm ci

COPY Frontend/ ./

# Same-origin deployment: the API is same-host, but this app is served under
# a path prefix (see deploy/edge/Caddyfile), so the browser's fetches must
# include that prefix too - a plain "/api/..." would resolve to the domain
# root, not this project's own API.
ENV VITE_API_URL=/projects/clinicbook/api
RUN npm run build

# Stage 2: serve the static output with Caddy
FROM caddy:2-alpine

COPY deploy/Caddyfile /etc/caddy/Caddyfile

# Vite outputs to "dist". Create React App outputs to "build" -
# change the source path below if you are on CRA.
COPY --from=build /app/dist /srv

EXPOSE 80
