# --------- BUILD STAGE ---------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Install required native toolchain for AOT
RUN apt-get update && \
    apt-get install -y clang zlib1g-dev libkrb5-dev && \
    rm -rf /var/lib/apt/lists/*

COPY PublicKeyServer ./PublicKeyServer 
COPY bc-csharp ./bc-csharp


RUN cd PublicKeyServer; dotnet publish PublicKeyServer.csproj -c Release -r linux-x64 --self-contained true -p:PublishSingleFile=true -o /app/out

# --------- RUNTIME STAGE ---------
FROM ubuntu:22.04 AS runtime
WORKDIR /app

# Install SSL dependencies for HTTPS
RUN apt-get update && \
    apt-get install -y ca-certificates libssl3 && \
    rm -rf /var/lib/apt/lists/*

# Copy AOT build output
COPY --from=build /app/out/ ./

# Remove debug symbol file
RUN rm -f PublicKeyServer.dbg

# Set executable permissions
RUN chmod +x PublicKeyServer

EXPOSE 5000
ENV ASPNETCORE_URLS=http://0.0.0.0:5000

ENTRYPOINT ["./PublicKeyServer"]

