FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["LeaderSurvey.sln", "./"]
COPY ["LeaderSurvey/LeaderSurvey.csproj", "LeaderSurvey/"]

# Restore dependencies
RUN dotnet restore "LeaderSurvey.sln"

# Copy the rest of the application files
COPY . .

# Publish the application
WORKDIR "/src/LeaderSurvey"
RUN dotnet publish "LeaderSurvey.csproj" -c Release -o /app/publish

# Final stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

# Copy the published application
COPY --from=build /app/publish . 

# Create a non-root user
RUN adduser -u 1000 --disabled-password --gecos "" appuser && chown -R appuser /app

USER appuser

ENTRYPOINT ["sh", "-c", "sleep 5 && dotnet LeaderSurvey.dll"]