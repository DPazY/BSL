# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Копируем csproj-файлы для кэширования слоев restore
COPY ["BSL.App/BSL.App.csproj", "BSL.App/"]
COPY ["BSL.Models/BSL.Models.csproj", "BSL.Models/"]
COPY ["BSL.Implementation/BSL.Implementation.csproj", "BSL.Implementation/"]
COPY ["BSL.Security/BSL.Security.csproj", "BSL.Security/"]

RUN dotnet restore "BSL.App/BSL.App.csproj"

# Копируем остальные исходники и собираем Release
COPY . .
WORKDIR "/src/BSL.App"
RUN dotnet publish "BSL.App.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Создаем рабочие директории для FileWatcher и данных
RUN mkdir -p /app/watch /app/data

COPY --from=build /app/publish .

EXPOSE 8080
ENTRYPOINT ["dotnet", "BSL.App.dll"]