# Use the official .NET 9.0 SDK image for building
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

# Set the working directory
WORKDIR /app

# Copy the solution file and project files
COPY Nomad.Api.sln ./
COPY Nomad.Api/Nomad.Api.csproj ./Nomad.Api/
COPY Nomad.Api.Tests/Nomad.Api.Tests.csproj ./Nomad.Api.Tests/

# Restore dependencies
RUN dotnet restore

# Copy the entire source code
COPY . .

# Build the application
WORKDIR /app/Nomad.Api
RUN dotnet build -c Release -o /app/build

# Publish the application
RUN dotnet publish -c Release -o /app/publish --no-restore

# Use the official .NET 9.0 runtime image for the final stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime

# Set the working directory
WORKDIR /app

# Copy the published application from the build stage
COPY --from=build /app/publish .

# Create a non-root user for security
RUN adduser --disabled-password --gecos '' appuser && chown -R appuser /app
USER appuser

# Expose the port that the application will run on
EXPOSE 8080

# Set environment variables for production
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080

# Set the entry point
ENTRYPOINT ["dotnet", "Nomad.Api.dll"]
