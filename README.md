# Introduction

Welcome in `OsmIntegrator` project.

# Run project

To run project execute following steps from project root folder.

## Install docker

Install docker from [here](https://docs.docker.com/desktop/).

## Run project locally

### Production environment values

Copy file `.env.production.sample` to `.env.production` and fill it with proper data

### Manage db

**Start db conatainer**

```bash
docker-compose up db
```

**Stop db container**

```bash
docker-compose down -v
```

Above command cleans volumens. 
It's usefull when you want to clear database.

## Run web api

```csharp
dotnet run -p osmintegrator
```

(make sure you are in root project folder)

### Check website

Project should work on following websites:
* Linux (Kestrel): `https://0.0.0.0:9998`
* Wndows (IISExpres): `http://localhost:44388`

# Examine the API with Postman

Download and install [Postman](https://www.postman.com/downloads/).
Postman collections are located in `postman` driectory.
* collection: `OsmIntegrator.postman_collection.json`
* environments for IISExpress: `osmintegrator_local_iis.postman_environment.json`
* environments for Kestrel: `osmintegrator_local_kestrel.postman_environment.json`
* environments for remote: `osmintegrator_local_remote.postman_environment.json`

# Documentation

Open project wiki by clicking on [this](https://github.com/technologiesforaccessibility/osmintegrator-wiki/wiki) link.
