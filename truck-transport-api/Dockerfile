FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG configuration=Release
WORKDIR /src
COPY ["truck-transport-api/TruckTransportApi.csproj", "truck-transport-api/"]
RUN dotnet restore "truck-transport-api/TruckTransportApi.csproj"
COPY . .
WORKDIR "/src/truck-transport-api"
RUN dotnet build "TruckTransportApi.csproj" -c $configuration -o /app/build

FROM build AS publish
ARG configuration=Release
RUN dotnet publish "TruckTransportApi.csproj" -c $configuration -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
USER root
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
USER app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TruckTransportApi.dll"]
