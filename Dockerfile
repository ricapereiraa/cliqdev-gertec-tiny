FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["OlistGertecIntegration.csproj", "./"]
RUN dotnet restore "OlistGertecIntegration.csproj"
COPY . .
WORKDIR "/src"
RUN dotnet build "OlistGertecIntegration.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "OlistGertecIntegration.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "OlistGertecIntegration.dll"]

