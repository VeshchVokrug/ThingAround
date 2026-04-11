FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy projects
COPY ["IdentityProfileService/IdentityProfileService.csproj", "IdentityProfileService/"]
COPY ["Core/Core.csproj", "Core/"]

# Restore dependencies
RUN dotnet restore "IdentityProfileService/IdentityProfileService.csproj"

# Build and publish
COPY . .
WORKDIR "/src/IdentityProfileService"
RUN dotnet publish "IdentityProfileService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS final
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Copy application files
COPY --from=build /app/publish .

# The Configs directory will be mounted as a volume at runtime to inject secrets
ENTRYPOINT ["dotnet", "IdentityProfileService.dll"]
