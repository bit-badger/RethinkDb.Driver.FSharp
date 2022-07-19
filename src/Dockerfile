FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["RethinkDb.Driver.FSharp.Tests/RethinkDb.Driver.FSharp.Tests.fsproj", "RethinkDb.Driver.FSharp.Tests/"]
RUN dotnet restore "RethinkDb.Driver.FSharp.Tests/RethinkDb.Driver.FSharp.Tests.fsproj"
COPY . .
WORKDIR "/src/RethinkDb.Driver.FSharp.Tests"
RUN dotnet build "RethinkDb.Driver.FSharp.Tests.fsproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "RethinkDb.Driver.FSharp.Tests.fsproj" -c Release -r linux-x64 --self-contained true -o /app/publish

FROM rethinkdb:latest AS test
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["RethinkDb.Driver.FSharp.Tests"]

# FROM base AS final
# WORKDIR /app
# COPY --from=publish /app/publish .
# ENTRYPOINT ["dotnet", "RethinkDb.Driver.FSharp.Tests.dll"]
