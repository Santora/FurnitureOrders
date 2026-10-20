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

# Zkopírování projektových souborů pro restore závislostí
COPY ["FurnitureOrders.Api/FurnitureOrders.Api.csproj", "FurnitureOrders.Api/"]
COPY ["FurnitureOrders.Shared/FurnitureOrders.Shared.csproj", "FurnitureOrders.Shared/"]
RUN dotnet restore "FurnitureOrders.Api/FurnitureOrders.Api.csproj"

# Zkopírování zbytku kódu a publikace API
COPY . .
WORKDIR "/src/FurnitureOrders.Api"
RUN dotnet publish "FurnitureOrders.Api.csproj" -c Release -o /app/api-publish /p:UseAppHost=false

# ========================================================
# Stage 3: Runtime Environment (Nginx + .NET 10 Web API)
# ========================================================
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Instalace webového serveru Nginx pro obsluhu statických souborů Blazoru
RUN apt-get update && \
    apt-get install -y nginx && \
    rm -rf /var/lib/apt/lists/*

# Zkopírování publikovaných binárek API
COPY --from=build-api /app/api-publish .

# Zkopírování statických souborů Blazor WASM do kořene Nginxu
COPY --from=build-client /app/client-publish/wwwroot /usr/share/nginx/html

# Zkopírování konfigurace Nginx reverzní proxy
COPY nginx/nginx.conf /etc/nginx/conf.d/default.conf

# Vystavení portu 80 (Nginx) a 8080 (.NET API vnitřně)
EXPOSE 80
EXPOSE 8080

# Spuštění Nginxu na pozadí a následně .NET API na popředí
CMD service nginx start && dotnet FurnitureOrders.Api.dll