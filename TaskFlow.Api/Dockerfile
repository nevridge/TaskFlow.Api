# Use the official .NET 9 SDK image for building
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project file from the correct path
COPY ["TaskFlow.Api/TaskFlow.Api.csproj", "TaskFlow.Api/"]
RUN dotnet restore "TaskFlow.Api/TaskFlow.Api.csproj"

# Copy the rest of the TaskFlow.Api folder
COPY TaskFlow.Api/ TaskFlow.Api/
WORKDIR /src/TaskFlow.Api
RUN dotnet publish "TaskFlow.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false --no-restore

# Use the official ASP.NET runtime image for final container
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Create directories for logs and data with proper permissions
RUN mkdir -p /app/logs /app/data && chmod 777 /app/logs /app/data

COPY --from=build /app/publish .

# Expose port and configure Kestrel
EXPOSE 8080
ENV ASPNETCORE_HTTP_PORTS=8080
ENV DOTNET_RUNNING_IN_CONTAINER=true
ENV ASPNETCORE_ENVIRONMENT=Production

# Define entry point
ENTRYPOINT ["dotnet", "TaskFlow.Api.dll"]