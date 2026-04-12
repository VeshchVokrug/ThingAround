FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy projects first for better restore caching
COPY ["Gateway/Gateway.csproj", "Gateway/"]
COPY ["Core/Core.csproj", "Core/"]
COPY ["Protos/Protos.csproj", "Protos/"]

RUN dotnet restore "Gateway/Gateway.csproj"

# Copy all source files and publish
COPY . .
WORKDIR "/src/Gateway"
RUN dotnet publish "Gateway.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS final
WORKDIR /app

EXPOSE 8080
EXPOSE 8081

COPY --from=build /app/publish .

# Public key can be supplied through a mounted Configs volume
ENTRYPOINT ["dotnet", "Gateway.dll"]

