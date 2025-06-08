

## Table of contents
- [Tech stack](#tech-stack)
- [Docker compose guide](#docker-compose-guide)

## Tech stack
Teknologistakken for applikationen er som følger:
- **C#**: Hovedprogrammeringssproget for applikationen.
- **ASP NET Core 8.0**: Hovedframeworket for applikationen.
- **Microsoft SQL Server**: Databasen til applikationen.
- **Entity Framework Core**: ORM, der bruges til at interagere med SQL Server.
- **MongoDB**: Databasen, der indeholder alle filmene.
- **MongoDB C# Driver**: Driveren, der bruges til at interagere med MongoDB.
- **Redis**: Databasen, der anvendes til at cache film og anmeldelser.
- **StackExchange.Redis**: Driveren, der bruges til at interagere med Redis.
- **Swagger**: Biblioteket, der bruges til at dokumentere API’et.
- **LINQ**: Biblioteket, der bruges til at skrive forespørgsler mod databasen.
- **SignalR**: Til at skubbe serverside data til forbudne klienter


## Docker compose guide
For at kører projektet lokalt, skal du have Docker installeret. 
Derefter skal du hente filen movies.7z inde fra dockercompose mappen - denne er gemt igennem Git LFS (Large File Storage) og kan derfor ikke hentes direkte ved kloning fra GitHub.
Denne fil skal pakkes ud i mappen `dockercompose` for at kunne seede MongoDB databasen.

Derefter kan du clone dette repository og køre følgende kommando i mappen dockercompose hvor `docker-compose.yml` filen er placeret:

```bash
docker-compose up --d
```
Herefter kan du tilgå applikationen på `http://localhost:5001/swagger/index.html` for at teste applikationen.
OBS: Husk at du skal have oprettet en bruger og logge ind for at teste endpoints.
Dette gør du igennem vores endpoints hvorefter du i øvre højre hjørne bruger swaggers "Authorize" knap til at genbruge den JWT token, der er genereret ved login.
Her skal du indsætte `Bearer  <din token>` i feltet for at bruge den.
