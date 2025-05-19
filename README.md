

## Table of contents
- [Table of contents](#table-of-contents)
- [Tech stack](#tech-stack)
- [MSSQL](#mssql)
  - [Isolation level, Transactions, og Locks.](#isolation-level-transactions-og-locks)
  - [Query optimization](#query-optimization)
  - [Stored procedures](#stored-procedures)
  - [Trigger](#Trigger)
- [MongoDB](#mongodb)
- [Redis](#redis)

## Tech stack
Teknologistakken for microservicen er som følger:
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
- 



## MSSQL

### Isolation level, Transactions, og Locks.


Disse strategier er implementeret for vores vores MSSQL database, hvilket er vores valgte relationelle database.

- **Isolation og locking**: Vi benytter SQL Server’s standard isolation level Read Committed, hvilket betyder, at vi kun læser committede data og undgår dirty reads, phantom reads og the lost update–problemet. Samtidig anvender vi SQL Server’s pessimistic locking, så de læste eller opdaterede rækker låses, indtil vores transaction er afsluttet. På den måde sikrer vi både dataintegritet og konsistens ved at forhindre, at andre sessions overskriver eller ændrer data, mens vi arbejder med dem.
  

- **Transactions**: Til alt der berører vores relationelle del af opgaven (MSSQL) gør vi brug af EF Core og dertil følger det at vi også gør brug af deres måde at håndtere transactions på. Dette betyder at alle database operationer bliver lavet som en transaction. Dette sikrer at alle ændringer bliver gemt i en transaction, og hvis der opstår en fejl, bliver alle ændringer rullet tilbage. Til at skrive vores transanctions gør vi brug af LINQ.

### Query optimization
indexing

### Stored procedures

### Trigger

## MongoDB

## Redis

- **Caching**:
Vi bruger primært Redis som caching-lag i løsningen ved at anvende et read-through pattern. Dette er med til at optimere ydeevnen og reducere belastningen på både vores MSSQL- og MongoDB-databaser.

Ofte anvendt data – som f.eks. watch lists, anmeldelser og film – caches midlertidigt i Redis med en TTL på 300 sekunder (5 minutter).

Når en klient foretager et GET-kald, forsøger løsningen først at hente data fra Redis. Hvis dataen ikke findes dér, hentes den fra den relevante database (SQL eller Mongo), hvorefter den caches i Redis til fremtidige forespørgsler, inden den returneres til klienten.

Denne tilgang reducerer svartider, skalerer bedre under høj belastning og giver en god balance mellem aktualitet og performance.  
- **Publish Subscribe**:
Vi bruger et reduceret publish-subscribe-system til vores watch party funktionalitet. Det betyder, at vi har channel-baseret messaging, hvor beskeder publiceres til en Redis-kanal, og alle klienter, der lytter via en SignalR-hub, modtager dem i realtid.

Til gengæld mangler vi funktioner som køsystem, persistens af beskeder, og avanceret filtrering og routing, som man f.eks. får med en løsning som RabbitMQ. Derfor lever systemet ikke helt op til det klassiske publish-subscribe-mønster, men fungerer fint til simple og hurtige realtids-scenarier som chat
