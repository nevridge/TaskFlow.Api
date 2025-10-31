# Use the official .NET 9 SDK image for building
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project files and restore dependencies
COPY ["TaskFlow.Api.csproj", "./"]
RUN dotnet restore "TaskFlow.Api.csproj"

# Copy everything else and build
COPY . .
RUN dotnet publish "TaskFlow.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false --no-restore

# Use the official ASP.NET runtime image for final container
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Expose port
EXPOSE 8080

# Define entry point
ENTRYPOINT ["dotnet", "TaskFlow.Api.dll"]
