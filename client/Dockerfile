# syntax=docker/dockerfile:1
# Build context: ./client
FROM node:24-alpine AS build
WORKDIR /app

COPY package.json package-lock.json ./
RUN npm ci --no-audit --no-fund

COPY . .
RUN npx ng build --configuration production

# The `application` builder emits dist/client/browser, the older `browser` builder dist/client.
RUN mkdir /out \
 && if [ -d dist/client/browser ]; then cp -r dist/client/browser/. /out/; else cp -r dist/client/. /out/; fi

FROM nginx:alpine AS final
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /out /usr/share/nginx/html
EXPOSE 80
