# Use the official .NET Core SDK as the build environment
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

# Set ASP.NET Core to listen on port 8080
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
ENV ASPNETCORE_ENVIRONMENT=Development

# Use the .NET Core SDK to build the project
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy the .csproj file and restore any dependencies
COPY ["SOFT_DB_EXAM.csproj", "./"]
RUN dotnet restore "./SOFT_DB_EXAM.csproj"

# Copy the rest of your source code
COPY . .

# Build the project
RUN dotnet build "SOFT_DB_EXAM.csproj" -c Release -o /app/build

# Publish the app to a directory for deployment
FROM build AS publish
RUN dotnet publish "SOFT_DB_EXAM.csproj" -c Release -o /app/publish

# Use the base image to run the app
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SOFT_DB_EXAM.dll"]