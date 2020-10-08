# If you want to build and test under Linux using a docker container, here's how:
#
#> docker build -t json-ld.net .
#> docker run --rm json-ld.net dotnet test -v normal

# .NET Core 2.1 on Ubuntu 18.04 LTS
FROM mcr.microsoft.com/dotnet/core/sdk:2.1-bionic

WORKDIR /App

# First we ONLY copy sln and csproj files so that we don't have to re-cache
# dotnet restore every time a .cs file changes
COPY src/json-ld.net/json-ld.net.csproj src/json-ld.net/json-ld.net.csproj
COPY test/json-ld.net.tests/json-ld.net.tests.csproj test/json-ld.net.tests/json-ld.net.tests.csproj
COPY JsonLD.sln JsonLD.sln
RUN dotnet restore

# Then we copy everything and run dotnet build
COPY . .
RUN dotnet build
