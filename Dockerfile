FROM mcr.microsoft.com/dotnet/sdk:10.0.401 AS build
WORKDIR /src

COPY global.json ./
COPY src/GetFast.Api/GetFast.Api.csproj src/GetFast.Api/
RUN dotnet restore src/GetFast.Api/GetFast.Api.csproj

COPY src/GetFast.Api/ src/GetFast.Api/
RUN dotnet publish src/GetFast.Api/GetFast.Api.csproj \
    --configuration Release --no-restore --output /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0.12 AS runtime
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080
COPY --from=build /app/publish ./
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "GetFast.Api.dll"]
