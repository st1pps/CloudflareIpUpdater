FROM mcr.microsoft.com/dotnet/runtime:7.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["src/Stipps.CloudflareIpUpdater/Stipps.CloudflareIpUpdater.csproj", "src/Stipps.CloudflareIpUpdater/"]
RUN dotnet restore "src/Stipps.CloudflareIpUpdater/Stipps.CloudflareIpUpdater.csproj"
COPY . .
WORKDIR "/src/src/Stipps.CloudflareIpUpdater"
RUN dotnet build "Stipps.CloudflareIpUpdater.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Stipps.CloudflareIpUpdater.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Stipps.CloudflareIpUpdater.dll"]
