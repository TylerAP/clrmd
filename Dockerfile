# run tests in a linux docker container
FROM microsoft/dotnet:2.1-sdk AS sdk-env

WORKDIR /app

COPY . .

RUN dotnet test src/Microsoft.Diagnostics.Runtime.Tests