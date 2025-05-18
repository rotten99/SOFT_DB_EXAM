

## Table of contents
- [Table of contents](#table-of-contents)
- [Tech stack](#tech-stack)
- [MSSQL](#mssql)
  - [Isolation level, Transactions, og Locks.](#isolation-level-transactions-og-locks)
  - [query optimization](#query-optimization)
  - [stored procedures](#stored-procedures)
  - [trigger](#trigger)
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

### query optimization
indexing

### stored procedures

### trigger

## MongoDB

## Redis
