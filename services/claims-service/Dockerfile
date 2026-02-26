# Multi-stage build: Build stage and Runtime stage
# Build stage: Compile the application
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS builder
WORKDIR /build

# Copy project file and restore dependencies
COPY services/claims-service/ClaimsService.csproj ./
RUN dotnet restore

# Copy application code
COPY services/claims-service/ ./

# Build the application
RUN dotnet build -c Release -o /build/out

# Publish the application
RUN dotnet publish -c Release -o /publish

# Runtime stage: Create final image with only runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app

# Copy published application from builder stage
COPY --from=builder /publish .

# Expose port 5115 (ASPNETCORE_HTTP_PORTS)
EXPOSE 5115

# Run the application
ENTRYPOINT ["dotnet", "ClaimsService.dll"]
