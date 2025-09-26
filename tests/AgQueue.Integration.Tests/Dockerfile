#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.
FROM postgres:13 AS base

RUN  apt-get update \
  && apt-get install -y wget \
  && rm -rf /var/lib/apt/lists/*

#Install .net runtime
RUN #set -eux; \
	wget https://packages.microsoft.com/config/debian/10/packages-microsoft-prod.deb -O packages-microsoft-prod.deb \
	dpkg -i packages-microsoft-prod.deb \
	apt-get update; \
	apt-get install -y apt-transport-https \
	apt-get update \
	apt-get install -y dotnet-sdk-9.0
	

#FROM mcr.microsoft.com/dotnet/runtime:9.0-bookworm-slim AS base
WORKDIR /app

#FROM mcr.microsoft.com/dotnet/sdk:9.0-bookworm-slim AS build
FROM postgres:13 AS build
WORKDIR /src
COPY ["tests/AgQueue.Integration.Tests/AgQueue.Integration.Tests.csproj", "tests/AgQueue.Integration.Tests/"]
COPY ["AgQueue.GrpcServer/AgQueue.GrpcServer.csproj", "AgQueue.GrpcServer/"]
COPY ["AgQueue.Server.Common/AgQueue.Server.Common.csproj", "AgQueue.Server.Common/"]
COPY ["AgQueue.Common/AgQueue.Common.csproj", "AgQueue.Common/"]
COPY ["AgQueue.Models/AgQueue.Models.csproj", "AgQueue.Models/"]
COPY ["AgQueue.Sqlite/AgQueue.Sqlite.csproj", "AgQueue.Sqlite/"]
RUN dotnet restore "tests/AgQueue.Integration.Tests/AgQueue.Integration.Tests.csproj"
COPY . .
WORKDIR "/src/tests/AgQueue.Integration.Tests"
RUN dotnet build "AgQueue.Integration.Tests.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "AgQueue.Integration.Tests.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AgQueue.Integration.Tests.dll"]