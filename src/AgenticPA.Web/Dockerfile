## Multi-stage build for AgenticPA.Web (.NET 10)
ARG DOTNET_VERSION=10.0
FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION} AS build
WORKDIR /src

## Copy solution + central package management first for layer cache
COPY Directory.Packages.props .
COPY AgenticPA.slnx .

## Copy projects
COPY src/AgenticPA.Services/        src/AgenticPA.Services/
COPY src/AgenticPA.McpServer/       src/AgenticPA.McpServer/
COPY src/AgenticPA.Agent/           src/AgenticPA.Agent/
COPY src/AgenticPA.ServiceDefaults/ src/AgenticPA.ServiceDefaults/
COPY src/AgenticPA.Web/             src/AgenticPA.Web/

## Copy data + skills
COPY data/   data/
COPY skills/ skills/

RUN dotnet restore src/AgenticPA.Web/AgenticPA.Web.csproj
RUN dotnet publish src/AgenticPA.Web/AgenticPA.Web.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_VERSION} AS runtime
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV DOTNET_RUNNING_IN_CONTAINER=true
ENV ASPNETCORE_ENVIRONMENT=Production

COPY --from=build /app/publish .

USER app
ENTRYPOINT ["dotnet", "AgenticPA.Web.dll"]
