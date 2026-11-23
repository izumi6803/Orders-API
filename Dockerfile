FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/OrdersApi.Api/OrdersApi.Api.csproj src/OrdersApi.Api/
RUN dotnet restore src/OrdersApi.Api/OrdersApi.Api.csproj

COPY src/ src/
WORKDIR /src/src/OrdersApi.Api
RUN dotnet publish -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "OrdersApi.Api.dll"]
