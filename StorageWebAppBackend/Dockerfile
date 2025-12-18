# ============================
# BUILD STAGE
# ============================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy solution file (if you have one)
COPY StorageWebAppBackend.sln ./

# Copy ONLY the real Web API project files first (for caching)
COPY StorageWebAppBackend/StorageWebAppBackend.csproj StorageWebAppBackend/

# Restore dependencies for the Web API project
RUN dotnet restore StorageWebAppBackend/StorageWebAppBackend.csproj

# Copy everything else
COPY . .

# Publish the Web API project only
RUN dotnet publish StorageWebAppBackend/StorageWebAppBackend.csproj \
    -c Release -o out

# ============================
# RUNTIME STAGE
# ============================
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

# Copy the published output from build stage
COPY --from=build /app/out .

# Bind to Railway port
ENV ASPNETCORE_URLS=http://0.0.0.0:${PORT}
EXPOSE 8080

# Start the Web API
CMD ["dotnet", "StorageWebAppBackend.dll"]
