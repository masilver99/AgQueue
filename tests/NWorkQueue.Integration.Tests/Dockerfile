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
	apt-get install -y dotnet-sdk-5.
	

#FROM mcr.microsoft.com/dotnet/runtime:5.0-buster-slim AS base
WORKDIR /app

#FROM mcr.microsoft.com/dotnet/sdk:5.0-buster-slim AS build
FROM postgres:13 AS build
WORKDIR /src
COPY ["tests/NWorkQueue.Integration.Tests/NWorkQueue.Integration.Tests.csproj", "tests/NWorkQueue.Integration.Tests/"]
COPY ["NWorkQueue.GrpcServer/NWorkQueue.GrpcServer.csproj", "NWorkQueue.GrpcServer/"]
COPY ["NWorkQueue.Server.Common/NWorkQueue.Server.Common.csproj", "NWorkQueue.Server.Common/"]
COPY ["NWorkQueue.Common/NWorkQueue.Common.csproj", "NWorkQueue.Common/"]
COPY ["NWorkQueue.Models/NWorkQueue.Models.csproj", "NWorkQueue.Models/"]
COPY ["NWorkQueue.Sqlite/NWorkQueue.Sqlite.csproj", "NWorkQueue.Sqlite/"]
RUN dotnet restore "tests/NWorkQueue.Integration.Tests/NWorkQueue.Integration.Tests.csproj"
COPY . .
WORKDIR "/src/tests/NWorkQueue.Integration.Tests"
RUN dotnet build "NWorkQueue.Integration.Tests.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "NWorkQueue.Integration.Tests.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "NWorkQueue.Integration.Tests.dll"]