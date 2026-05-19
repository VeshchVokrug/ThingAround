FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["backend_C_sharp/CatalogService/src/Catalog.Presentation/Catalog.Presentation.csproj", "backend_C_sharp/CatalogService/src/Catalog.Presentation/"]
COPY ["backend_C_sharp/CatalogService/src/Catalog.Application/Catalog.Application.csproj", "backend_C_sharp/CatalogService/src/Catalog.Application/"]
COPY ["backend_C_sharp/CatalogService/src/Catalog.Infrastructure/Catalog.Infrastructure.csproj", "backend_C_sharp/CatalogService/src/Catalog.Infrastructure/"]
COPY ["backend_C_sharp/CatalogService/src/Catalog.Domain/Catalog.Domain.csproj", "backend_C_sharp/CatalogService/src/Catalog.Domain/"]
COPY ["backend_C_sharp/CatalogService/src/Catalog.Contracts/Catalog.Contracts.csproj", "backend_C_sharp/CatalogService/src/Catalog.Contracts/"]
COPY ["backend_C_sharp/Core/Core.csproj", "backend_C_sharp/Core/"]
COPY ["Protos/Protos.csproj", "Protos/"]

RUN dotnet restore "backend_C_sharp/CatalogService/src/Catalog.Presentation/Catalog.Presentation.csproj"

COPY ["backend_C_sharp/CatalogService/", "backend_C_sharp/CatalogService/"]
COPY ["backend_C_sharp/Core/", "backend_C_sharp/Core/"]
COPY ["Protos/", "Protos/"]

WORKDIR "/src/backend_C_sharp/CatalogService/src/Catalog.Presentation"
RUN dotnet publish "Catalog.Presentation.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS final
WORKDIR /app

EXPOSE 8080
EXPOSE 8081

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "Catalog.Presentation.dll"]

