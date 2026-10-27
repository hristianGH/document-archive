FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY src/Shared/Shared.csproj src/Shared/
COPY src/Export/Export.Core/Export.Core.csproj src/Export/Export.Core/
COPY src/Export/Export.Data/Export.Data.csproj src/Export/Export.Data/
COPY src/Export/Export.Api/Export.Api.csproj src/Export/Export.Api/
RUN dotnet restore src/Export/Export.Api/Export.Api.csproj
COPY src/Shared/ src/Shared/
COPY src/Export/Export.Core/ src/Export/Export.Core/
COPY src/Export/Export.Data/ src/Export/Export.Data/
COPY src/Export/Export.Api/ src/Export/Export.Api/
RUN dotnet publish src/Export/Export.Api/Export.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
USER app
EXPOSE 8080
ENTRYPOINT ["dotnet", "Export.Api.dll"]
