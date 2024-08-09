FROM microsoft/dotnet:2.1-aspnetcore-runtime AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM microsoft/dotnet:2.1-sdk AS build
WORKDIR /src
COPY ["nkv.MicroService.API/nkv.MicroService.API.csproj", "nkv.MicroService.API/"]
RUN dotnet restore "nkv.MicroService.API/nkv.MicroService.API.csproj"
COPY . .
WORKDIR "/src/nkv.MicroService.API"
RUN dotnet build "nkv.MicroService.API.csproj" -c Release -o /app

FROM build AS publish
RUN dotnet publish "nkv.MicroService.API.csproj" -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "nkv.MicroService.API.dll"]