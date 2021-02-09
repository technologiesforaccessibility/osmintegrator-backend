# Introduction

Welcome in `OsmIntegrator` project.

# Run project

To run project execute following steps from project root folder.

### Install docker

Install docker from [here](https://docs.docker.com/desktop/).

### Run database

```bash
cd osmintegrator
docker-compose up
```

### Run web api

```csharp
dotnet run -p osmintegrator
```

(make sure you are in root project folder)

### Check website

Project should work on following websites:
* Linux (Kestrel): `https://0.0.0.0:9999`
* Wndows (IISExpres): ``

# Examine the API with Postman

Download and install [Postman](https://www.postman.com/downloads/).

Import

# Documentation

Open project wiki by clicking on [this](https://github.com/technologiesforaccessibility/osmintegrator-wiki/wiki) link.
