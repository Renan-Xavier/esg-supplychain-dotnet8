FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["src/EsgSupplyChain.Api/EsgSupplyChain.Api.csproj", "src/EsgSupplyChain.Api/"]
RUN dotnet restore "src/EsgSupplyChain.Api/EsgSupplyChain.Api.csproj"

COPY ["src/EsgSupplyChain.Api/", "src/EsgSupplyChain.Api/"]
WORKDIR "/src/src/EsgSupplyChain.Api"
RUN dotnet publish "EsgSupplyChain.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "EsgSupplyChain.Api.dll"]
