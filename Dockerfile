# sdk required to build ASP.NET app
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /source


COPY *.sln .
COPY OsmIntegrator/*.csproj ./OsmIntegrator/

COPY OsmIntegrator/. ./OsmIntegrator/
RUN cd OsmIntegrator \ 
    && dotnet add package Microsoft.EntityFrameworkCore.Analyzers --version 6.0.1 \
    && dotnet publish -c release -o /dist --no-restore

# runtime environment for ASP.NET
FROM mcr.microsoft.com/dotnet/aspnet:6.0
WORKDIR /app
COPY --from=build /dist ./
ENTRYPOINT ["dotnet", "osmintegrator.dll"]
