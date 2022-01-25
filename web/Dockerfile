#Build
FROM node:lts-alpine AS build-env
WORKDIR /App
COPY package*.json ./
RUN npm install
COPY . .
RUN npm run build

#Run
FROM nginx:stable-alpine
COPY --from=build-env /App/dist /usr/share/nginx/html
# COPY /dist /usr/share/nginx/html
EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]