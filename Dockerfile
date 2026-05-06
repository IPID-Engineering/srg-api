FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src

COPY SRG.sln ./
COPY SRG.Domain/SRG.Domain.csproj SRG.Domain/
COPY SRG.Application/SRG.Application.csproj SRG.Application/
COPY SRG.Infrastructure/SRG.Infrastructure.csproj SRG.Infrastructure/
COPY SRG.Api/SRG.Api.csproj SRG.Api/

RUN dotnet restore SRG.sln

COPY . .
RUN dotnet publish SRG.Api/SRG.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0

WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 5000

ENTRYPOINT ["dotnet", "SRG.Api.dll"]
