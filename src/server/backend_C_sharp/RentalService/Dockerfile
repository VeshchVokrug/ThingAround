FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["backend_C_sharp/RentalService/src/RentalService.Presentation/RentalService.Presentation.csproj", "backend_C_sharp/RentalService/src/RentalService.Presentation/"]
COPY ["backend_C_sharp/RentalService/src/RentalService.Application/RentalService.Application.csproj", "backend_C_sharp/RentalService/src/RentalService.Application/"]
COPY ["backend_C_sharp/RentalService/src/RentalService.Infrastructure/RentalService.Infrastructure.csproj", "backend_C_sharp/RentalService/src/RentalService.Infrastructure/"]
COPY ["backend_C_sharp/RentalService/src/RentalService.Domain/RentalService.Domain.csproj", "backend_C_sharp/RentalService/src/RentalService.Domain/"]
COPY ["backend_C_sharp/RentalService/src/RentalService.Infrastructure.Abstractions/RentalService.Infrastructure.Abstractions.csproj", "backend_C_sharp/RentalService/src/RentalService.Infrastructure.Abstractions/"]
COPY ["backend_C_sharp/Core/Core.csproj", "backend_C_sharp/Core/"]
COPY ["Protos/Protos.csproj", "Protos/"]

RUN dotnet restore "backend_C_sharp/RentalService/src/RentalService.Presentation/RentalService.Presentation.csproj"

COPY ["backend_C_sharp/RentalService/", "backend_C_sharp/RentalService/"]
COPY ["backend_C_sharp/Core/", "backend_C_sharp/Core/"]
COPY ["Protos/", "Protos/"]

WORKDIR "/src/backend_C_sharp/RentalService/src/RentalService.Presentation"
RUN dotnet publish "RentalService.Presentation.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS final
WORKDIR /app

EXPOSE 8080
EXPOSE 8081

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "RentalService.Presentation.dll"]