FROM mcr.microsoft.com/dotnet/core/sdk:2.2 AS build-env

WORKDIR /app

COPY *.sln ./

COPY ColorBot/*.csproj ./ColorBot/

RUN dotnet restore -s https://nuget.emzi0767.com/api/v3/index.json -s https://api.nuget.org/v3/index.json

COPY . ./

RUN dotnet publish -c Release

FROM mcr.microsoft.com/dotnet/core/runtime:2.2

COPY --from=build-env /app/ColorBot/bin/Release/netcoreapp2.2/publish app/

RUN ls app

ENTRYPOINT ["dotnet", "app/ColorBot.dll"]
