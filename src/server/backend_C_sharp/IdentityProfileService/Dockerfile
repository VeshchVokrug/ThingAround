FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy projects
COPY ["backend_C_sharp/IdentityProfileService/IdentityProfileService.csproj", "backend_C_sharp/IdentityProfileService/"]
COPY ["backend_C_sharp/Core/Core.csproj", "backend_C_sharp/Core/"]
COPY ["Protos/Protos.csproj", "Protos/"]

# Restore dependencies
RUN dotnet restore "backend_C_sharp/IdentityProfileService/IdentityProfileService.csproj"

# Build and publish
COPY ["backend_C_sharp/IdentityProfileService/", "backend_C_sharp/IdentityProfileService/"]
COPY ["backend_C_sharp/Core/", "backend_C_sharp/Core/"]
COPY ["Protos/", "Protos/"]
WORKDIR "/src/backend_C_sharp/IdentityProfileService"
RUN dotnet publish "IdentityProfileService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS final
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Copy application files
COPY --from=build /app/publish .

# The Configs directory will be mounted as a volume at runtime to inject secrets
ENTRYPOINT ["dotnet", "IdentityProfileService.dll"]
