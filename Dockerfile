FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["Directory.Build.props", "."]
COPY ["Directory.Packages.props", "."]
COPY ["src/Esperanca.Worker.Service/Esperanca.Worker.Service.csproj", "src/Esperanca.Worker.Service/"]

RUN dotnet restore "src/Esperanca.Worker.Service/Esperanca.Worker.Service.csproj"

COPY . .

RUN dotnet publish "src/Esperanca.Worker.Service/Esperanca.Worker.Service.csproj" \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "Esperanca.Worker.Service.dll"]