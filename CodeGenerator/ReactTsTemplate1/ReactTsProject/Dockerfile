# stage1 as builder
FROM node:alpine as builder

WORKDIR /nextjs-ui

# copy the package.json to install dependencies
COPY package.json package-lock.json ./

# Install the dependencies and make the folder
RUN npm install --force 

COPY . .

# Build the project and copy the files
ENV NODE_OPTIONS="--max-old-space-size=16000"
RUN npm run build


FROM nginx:alpine

EXPOSE 80
#!/bin/sh

COPY ./default.conf /etc/nginx/conf.d/default.conf
# Copy from the stahg 1
COPY --from=builder /nextjs-ui/build /usr/share/nginx/html