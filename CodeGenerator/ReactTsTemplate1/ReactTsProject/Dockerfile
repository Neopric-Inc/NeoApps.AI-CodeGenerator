# Stage 1: Build the React application
FROM node:alpine as builder

# Set the working directory inside the container
WORKDIR /nextjs-ui

# Copy the package.json and package-lock.json
COPY package.json package-lock.json ./

# Install the dependencies
RUN npm install --force

# Copy the entire project into the container
COPY . .

# Build the application
ENV NODE_OPTIONS="--max-old-space-size=16000"
RUN npm run build

# Stage 2: Serve the built application
FROM node:alpine

# Install `serve` to serve static files
RUN npm install -g serve

# Set the working directory
WORKDIR /nextjs-ui

# Copy the build output from the previous stage
COPY --from=builder /nextjs-ui/build /nextjs-ui/build

# Expose port 3000
EXPOSE 3000

# Command to serve the static files
CMD ["serve", "-s", "build", "-l", "3000"]
