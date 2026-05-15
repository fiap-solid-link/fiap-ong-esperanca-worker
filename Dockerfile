# ============================================
# Stage 1: Build (SDK - compilação e publish)
# ============================================
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src

# 0. Copiar arquivos de configuração centralizados
# Precisam estar disponíveis ANTES do restore para evitar erro NETSDK1013
COPY ["Directory.Build.props", "."]
COPY ["Directory.Packages.props", "."]

# 1. Copiar apenas arquivos .csproj
# Estratégia de cache: se apenas código .cs mudar, essa layer é reutilizada
# Resultado: restore de pacotes NuGet não é refeito desnecessariamente
COPY ["src/Esperanca.Campanha.Domain/Esperanca.Campanha.Domain.csproj", "src/Esperanca.Campanha.Domain/"]
COPY ["src/Esperanca.Campanha.Application/Esperanca.Campanha.Application.csproj", "src/Esperanca.Campanha.Application/"]
COPY ["src/Esperanca.Campanha.Infrastructure/Esperanca.Campanha.Infrastructure.csproj", "src/Esperanca.Campanha.Infrastructure/"]
COPY ["src/Esperanca.Campanha.WebApi/Esperanca.Campanha.WebApi.csproj", "src/Esperanca.Campanha.WebApi/"]

# 2. Restore de pacotes NuGet
# Layer cacheada — só é refeita se os .csproj ou props mudarem
RUN dotnet restore "src/Esperanca.Campanha.WebApi/Esperanca.Campanha.WebApi.csproj"

# 3. Copiar todo o código-fonte restante
# Feito APÓS o restore para maximizar reuso de cache
COPY . .

# 4. Publish da aplicação
# -c Release: modo otimizado para produção
# -o /app/publish: diretório de saída dos binários
# /p:UseAppHost=false: não gera executável nativo (reduz tamanho)
# /p:PublishTrimmed=false: mantém compatibilidade com MediatR/reflection
RUN dotnet publish "src/Esperanca.Campanha.WebApi/Esperanca.Campanha.WebApi.csproj" \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false \
    /p:PublishTrimmed=false

# ============================================
# Stage 2: Runtime (ASP.NET Alpine - super leve)
# ============================================
# Alpine (~60 MB vs ~220 MB do Debian)
# Não precisa de SDK — apenas runtime .NET
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS final

# Criar usuário não-root (segurança)
RUN addgroup -g 1000 appgroup && \
    adduser -u 1000 -G appgroup -s /bin/sh -D appuser

WORKDIR /app

# Copiar APENAS os binários publicados do stage anterior
COPY --from=build /app/publish .

# Ajustar permissões
RUN chown -R appuser:appgroup /app

# Trocar para usuário não-root
USER appuser

# Porta 8080 (padrão para containers não-root)
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080

ENTRYPOINT ["dotnet", "Esperanca.Campanha.WebApi.dll"]
