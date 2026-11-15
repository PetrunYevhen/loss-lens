FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Restore first so the package layer is cached until a project file changes.
COPY Directory.Build.props ./
COPY src/LossLens.Core/*.csproj src/LossLens.Core/
COPY src/LossLens.Application/*.csproj src/LossLens.Application/
COPY src/LossLens.Infrastructure/*.csproj src/LossLens.Infrastructure/
COPY src/LossLens.Api/*.csproj src/LossLens.Api/
RUN dotnet restore src/LossLens.Api/LossLens.Api.csproj

COPY src ./src
RUN dotnet publish src/LossLens.Api/LossLens.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app .
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "LossLens.Api.dll"]
