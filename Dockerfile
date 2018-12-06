# run tests in a linux docker container
FROM microsoft/dotnet:2.1-sdk AS sdk-env

WORKDIR /app

RUN dotnet tool install -g dotnet-symbol

COPY . .

RUN dotnet restore src/Microsoft.Diagnostics.Runtime.Tests

ENV LD_LIBRARY_PATH=/usr/share/dotnet/shared/Microsoft.NETCore.App/2.1.6/

ENTRYPOINT [ \
    "dotnet", "test", \
    "--blame", \
    "-v", "n", \
    "--filter", "AttachTests", \
    "src/Microsoft.Diagnostics.Runtime.Tests", \
    "--", "--verbose" \
]