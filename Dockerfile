# Set the major version of dotnet
ARG DOTNET_VERSION=10.0

# Stage 1 - Build the app using the dotnet SDK
FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION}-azurelinux3.0 AS build
WORKDIR /build

COPY Dfe.Unified.Intake.csproj ./
RUN dotnet restore "./Dfe.Unified.Intake.csproj"

COPY . ./
RUN dotnet publish "./Dfe.Unified.Intake.csproj" \
    --configuration Release \
    --no-restore \
    --output /app

# Stage 2 - Build a runtime environment
FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_VERSION}-azurelinux3.0 AS final
WORKDIR /app

COPY --from=build /app ./
COPY ./script/docker-entrypoint.sh /app/docker-entrypoint.sh
RUN chmod +x ./docker-entrypoint.sh

USER $APP_UID
