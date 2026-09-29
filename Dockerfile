# Build:  docker build -t eventease .
# Run:    docker run -p 8080:8080 eventease
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY EventEase/EventEase.csproj EventEase/
RUN dotnet restore EventEase/EventEase.csproj
COPY EventEase/ EventEase/
RUN dotnet publish EventEase/EventEase.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
ENV ASPNETCORE_ENVIRONMENT=Production
COPY --from=build /app/publish .
USER app
EXPOSE 8080
ENTRYPOINT ["dotnet", "EventEase.dll"]
