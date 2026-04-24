FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy projects first for better restore caching
COPY ["backend_C_sharp/Gateway/Gateway.csproj", "backend_C_sharp/Gateway/"]
COPY ["backend_C_sharp/Core/Core.csproj", "backend_C_sharp/Core/"]
COPY ["Protos/Protos.csproj", "Protos/"]

RUN dotnet restore "backend_C_sharp/Gateway/Gateway.csproj"

# Copy only required source trees from the server root context
COPY ["backend_C_sharp/Gateway/", "backend_C_sharp/Gateway/"]
COPY ["backend_C_sharp/Core/", "backend_C_sharp/Core/"]
COPY ["Protos/", "Protos/"]
WORKDIR "/src/backend_C_sharp/Gateway"
RUN dotnet publish "Gateway.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS final
WORKDIR /app

EXPOSE 8080
EXPOSE 8081

COPY --from=build /app/publish .

# Public key can be supplied through a mounted Configs volume
ENTRYPOINT ["dotnet", "Gateway.dll"]

