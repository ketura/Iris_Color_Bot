FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build-env

WORKDIR /app

COPY *.sln ./

COPY ColorBot/*.csproj ./ColorBot/

RUN dotnet restore -s https://nuget.emzi0767.com/api/v3/index.json -s https://api.nuget.org/v3/index.json

COPY . ./

RUN dotnet publish -c Release

FROM mcr.microsoft.com/dotnet/core/runtime:3.1

COPY --from=build-env /app/ColorBot/bin/Release/netcoreapp3.1/publish app/

RUN ls app

ENTRYPOINT ["dotnet", "app/ColorBot.dll"]
