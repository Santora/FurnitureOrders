# ========================================================
# Stage 1: Build Blazor WebAssembly Frontend
# ========================================================
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build-client
WORKDIR /src

# Zkopírování projektových souborů pro restore závislostí
COPY ["FurnitureOrders.Client/FurnitureOrders.Client.csproj", "FurnitureOrders.Client/"]
COPY ["FurnitureOrders.Shared/FurnitureOrders.Shared.csproj", "FurnitureOrders.Shared/"]
RUN dotnet restore "FurnitureOrders.Client/FurnitureOrders.Client.csproj"

# Zkopírování zbytku kódu a publikace klienta
COPY . .
WORKDIR "/src/FurnitureOrders.Client"
RUN dotnet publish "FurnitureOrders.Client.csproj" -c Release -o /app/client-publish

# ========================================================
# Stage 2: Build ASP.NET Core API Backend
# ========================================================
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build-api
WORKDIR /src

COPY ["FurnitureOrders.Api/FurnitureOrders.Api.csproj", "FurnitureOrders.Api/"]
COPY ["FurnitureOrders.Shared/FurnitureOrders.Shared.csproj", "FurnitureOrders.Shared/"]
RUN dotnet restore "FurnitureOrders.Api/FurnitureOrders.Api.csproj"

COPY . .
WORKDIR "/src/FurnitureOrders.Api"
RUN dotnet publish "FurnitureOrders.Api.csproj" -c Release -o /app/api-publish /p:UseAppHost=false

# ========================================================
# Stage 3: Runtime (Nginx + .NET 10 Web API v jednom kontejneru)
# ========================================================
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Nginx servíruje statické soubory Blazoru a směruje /api/ na API
RUN apt-get update && \
    apt-get install -y --no-install-recommends nginx && \
    rm -rf /var/lib/apt/lists/* /etc/nginx/sites-enabled/default

COPY --from=build-api /app/api-publish .
COPY --from=build-client /app/client-publish/wwwroot /usr/share/nginx/html
COPY nginx/nginx.conf /etc/nginx/conf.d/default.conf

# API poslouchá jen uvnitř kontejneru; ven je dostupný pouze Nginx na portu 80
ENV ASPNETCORE_URLS=http://127.0.0.1:8080
EXPOSE 80

# Nginx na pozadí, API jako hlavní proces (správně reaguje na zastavení kontejneru)
CMD ["sh", "-c", "nginx && exec dotnet FurnitureOrders.Api.dll"]
