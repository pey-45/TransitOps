FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY TransitOps.Api/TransitOps.Api.csproj TransitOps.Api/
RUN dotnet restore TransitOps.Api/TransitOps.Api.csproj
COPY TransitOps.Api/ TransitOps.Api/
RUN dotnet publish TransitOps.Api/TransitOps.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
RUN apt-get update \
    && apt-get install -y --no-install-recommends libgssapi-krb5-2 \
    && rm -rf /var/lib/apt/lists/*
COPY --from=build /app .
EXPOSE 8080
ENTRYPOINT ["dotnet", "TransitOps.Api.dll"]
