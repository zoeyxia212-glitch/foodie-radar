# ---------- build ----------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Restore first so the package layer is cached between code changes
COPY FoodieRadar.Api/FoodieRadar.Api.csproj FoodieRadar.Api/
RUN dotnet restore FoodieRadar.Api/FoodieRadar.Api.csproj

COPY FoodieRadar.Api/ FoodieRadar.Api/
RUN dotnet publish FoodieRadar.Api/FoodieRadar.Api.csproj -c Release -o /app --no-restore

# ---------- runtime ----------
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .

# .NET 8 images listen on 8080 and ship a non-root "app" user
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
USER $APP_UID

ENTRYPOINT ["dotnet", "FoodieRadar.Api.dll"]
