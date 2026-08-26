# Sarafan Core

[![ci](https://github.com/maxirmx/sarafan.core/actions/workflows/ci.yml/badge.svg)](https://github.com/maxirmx/sarafan.core/actions/workflows/ci.yml)
[![publish](https://github.com/maxirmx/sarafan.core/actions/workflows/publish.yml/badge.svg)](https://github.com/maxirmx/sarafan.core/actions/workflows/publish.yml)

Empty ASP.NET Core service for the Sarafan system. The project targets .NET 10 LTS and is packaged as a Linux container.

## Prerequisites

- .NET 10 SDK
- Docker with Docker Compose

## Local development

```bash
dotnet restore Sarafan.sln
dotnet run --project src/Sarafan.Core/Sarafan.Core.csproj
```

The status endpoint is available at <http://localhost:5080/api/status/status> when the development launch profile is used.

## Docker

```bash
docker compose up --build
```

The containerized API is available at <http://localhost:8080/api/status/status>.

## Verification

```bash
dotnet build Sarafan.sln --configuration Release
docker compose up -d --build --wait
curl --fail http://localhost:8080/api/status/status
docker compose down
```
