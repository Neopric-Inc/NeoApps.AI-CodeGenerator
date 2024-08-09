# First stage: Build the .NET application
FROM ubuntu:20.04 AS install-env
RUN apt-get update && \
    apt-get install -y apt-transport-https \
    ca-certificates \
    curl \
    gnupg-agent \
    software-properties-common && \
    apt-get update && \
    apt-get install -y git && \
    apt-get update && apt-get install -y wget && \
    wget https://packages.microsoft.com/config/debian/11/packages-microsoft-prod.deb -O packages-microsoft-prod.deb && \
    dpkg -i packages-microsoft-prod.deb && \
    apt-get update && \
    apt-get install -y dotnet-sdk-6.0 dotnet-runtime-6.0

RUN dotnet tool install --global Swashbuckle.AspNetCore.Cli --version 6.5.0

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build-env

ENV PARAMETER ""
WORKDIR /app
COPY ./ /app
RUN dotnet restore
RUN dotnet build -c Release
RUN dotnet publish -c Release -o out
# Third stage: Run the .NET application
FROM mcr.microsoft.com/dotnet/sdk:6.0
WORKDIR /app

COPY --from=build-env /app /app
COPY --from=build-env /app/out /app/out
ENTRYPOINT ["dotnet", "/app/out/NoCodeAppGenerator.dll"]
