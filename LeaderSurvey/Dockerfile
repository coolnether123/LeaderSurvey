# Use Linux-compatible .NET 8.0 SDK image
FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /app

# Copy project and restore
COPY LeaderSurvey/*.csproj ./LeaderSurvey/
RUN dotnet restore LeaderSurvey/LeaderSurvey.csproj

# Copy everything else and publish
COPY . ./
RUN dotnet publish LeaderSurvey/LeaderSurvey.csproj -c Release -o /app/out

# Final runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS runtime
WORKDIR /app

# Copy published files
COPY --from=build /app/out/ ./

# Expose the ports the app will run on
EXPOSE 80
EXPOSE 8080

# Environment variable for ASP.NET Core
ENV ASPNETCORE_URLS=http://+:80;http://+:8080

# Start the application
ENTRYPOINT ["dotnet", "LeaderSurvey.dll"]
