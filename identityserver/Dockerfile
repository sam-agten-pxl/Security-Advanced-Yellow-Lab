#Build step
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build-env
WORKDIR /App

COPY *.csproj ./
RUN dotnet restore

COPY . ./
RUN dotnet publish -c Release -o out

#Run api
FROM mcr.microsoft.com/dotnet/aspnet:3.1
#COPY bin/Release/net5.0/publish/ App/
WORKDIR /App

COPY --from=build-env /App/out .
EXPOSE 80
ENTRYPOINT ["dotnet", "IdentityServer.dll"]