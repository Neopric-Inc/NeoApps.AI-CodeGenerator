# syntax=docker/dockerfile:1
# Containerised generator. Mount a config and an output folder:
#   docker build -t neoapps-generator CodeGenerator
#   docker run --rm -v "$PWD/samples:/samples" -v "$PWD/out:/out" neoapps-generator --config /samples/generator.backend.json
# Generated apps are written next to the generator (/out when mounted as below).
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY NoCodeAppGenerator.csproj .
RUN dotnet restore
COPY . .
RUN dotnet publish NoCodeAppGenerator.csproj -c Release -o /app/bin --no-restore

# The SDK is needed at runtime: the generator restores/builds the generated solution to validate it.
FROM mcr.microsoft.com/dotnet/sdk:10.0
WORKDIR /out/CodeGenerator
COPY --from=build /src/DotNetMySQLTemplate ./DotNetMySQLTemplate
COPY --from=build /src/ReactTsTemplate1 ./ReactTsTemplate1
COPY --from=build /src/ReactTsTemplate2 ./ReactTsTemplate2
COPY --from=build /src/ReactTsTemplate3 ./ReactTsTemplate3
COPY --from=build /src/ReactTsTemplate4 ./ReactTsTemplate4
COPY --from=build /src/appsettings.json ./
COPY --from=build /app/bin /app/bin
ENV NEOAPPS_GENERATOR_ROOT=/out/CodeGenerator
ENTRYPOINT ["dotnet", "/app/bin/NoCodeAppGenerator.dll"]
