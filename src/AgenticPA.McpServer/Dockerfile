ARG DOTNET_VERSION=10.0
FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION} AS build
WORKDIR /src

COPY Directory.Packages.props .
COPY AgenticPA.slnx .
COPY src/AgenticPA.Services/        src/AgenticPA.Services/
COPY src/AgenticPA.ServiceDefaults/ src/AgenticPA.ServiceDefaults/
COPY src/AgenticPA.McpServer/       src/AgenticPA.McpServer/
COPY data/                          data/

RUN dotnet restore src/AgenticPA.McpServer/AgenticPA.McpServer.csproj
RUN dotnet publish src/AgenticPA.McpServer/AgenticPA.McpServer.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_VERSION} AS runtime
WORKDIR /app
EXPOSE 7070
ENV ASPNETCORE_URLS=http://+:7070
ENV DOTNET_RUNNING_IN_CONTAINER=true
ENV ASPNETCORE_ENVIRONMENT=Production

COPY --from=build /app/publish .

USER app
ENTRYPOINT ["dotnet", "AgenticPA.McpServer.dll"]
