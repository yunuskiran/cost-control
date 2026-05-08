FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /source

COPY TokenGuard.slnx .
COPY src/TokenGuard.Core/TokenGuard.Core.csproj src/TokenGuard.Core/
COPY src/TokenGuard.Infrastructure/TokenGuard.Infrastructure.csproj src/TokenGuard.Infrastructure/
COPY src/TokenGuard.Api/TokenGuard.Api.csproj src/TokenGuard.Api/
RUN dotnet restore src/TokenGuard.Api/TokenGuard.Api.csproj

COPY src/ src/
RUN dotnet publish src/TokenGuard.Api/TokenGuard.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "TokenGuard.Api.dll"]
