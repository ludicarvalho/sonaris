FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Sonaris.Backend.csproj", "."]
RUN dotnet restore "./Sonaris.Backend.csproj"
COPY . .
RUN dotnet publish "./Sonaris.Backend.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 7071
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Sonaris.Backend.dll"]
