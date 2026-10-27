FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY src/Shared/Shared.csproj src/Shared/
COPY src/Archive/Archive.Core/Archive.Core.csproj src/Archive/Archive.Core/
COPY src/Archive/Archive.Data/Archive.Data.csproj src/Archive/Archive.Data/
COPY src/Archive/Archive.Api/Archive.Api.csproj src/Archive/Archive.Api/
RUN dotnet restore src/Archive/Archive.Api/Archive.Api.csproj
COPY src/Shared/ src/Shared/
COPY src/Archive/Archive.Core/ src/Archive/Archive.Core/
COPY src/Archive/Archive.Data/ src/Archive/Archive.Data/
COPY src/Archive/Archive.Api/ src/Archive/Archive.Api/
RUN dotnet publish src/Archive/Archive.Api/Archive.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
RUN mkdir -p /data/files && chown app:app /data/files
COPY --from=build /app .
COPY fixtures/cabinets.json config/cabinets.json
ENV Cabinets__Path=/app/config/cabinets.json
USER app
EXPOSE 8080
ENTRYPOINT ["dotnet", "Archive.Api.dll"]
