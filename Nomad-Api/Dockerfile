# Use the official .NET 9.0 SDK image for building
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

# Set the working directory
WORKDIR /app

# Copy the main project file first for better caching
COPY Nomad.Api/Nomad.Api.csproj ./Nomad.Api/

# Restore dependencies for the main project
WORKDIR /app/Nomad.Api
RUN dotnet restore

# Copy the entire source code
WORKDIR /app
COPY . .

# Build and publish the application
WORKDIR /app/Nomad.Api
RUN dotnet publish -c Release -o /app/publish --no-restore

# Use the official .NET 9.0 runtime image for the final stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime

# Set the working directory
WORKDIR /app

# Copy the published application from the build stage
COPY --from=build /app/publish .
# Copy Templates directory manually
COPY Nomad.Api/Templates ./Templates

# Install Puppeteer dependencies
USER root
RUN apt-get update && apt-get install -y \
    libglib2.0-0 \
    libnss3 \
    libatk1.0-0 \
    libatk-bridge2.0-0 \
    libcups2 \
    libdrm2 \
    libxkbcommon0 \
    libxcomposite1 \
    libxdamage1 \
    libxext6 \
    libxfixes3 \
    libxrandr2 \
    libgbm1 \
    libasound2 \
    && rm -rf /var/lib/apt/lists/*

# Create a non-root user for security
RUN adduser --disabled-password --gecos '' appuser && chown -R appuser /app
USER appuser

# Expose the port that the application will run on
# Port will be dynamically assigned by Render

# Set environment variables for production
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:$PORT

# Set the entry point
ENTRYPOINT ["dotnet", "Nomad.Api.dll"]
