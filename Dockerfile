# Use official .NET Core SDK as the build environment
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

# Use .NET SDK for build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["Tech_Store.csproj", "./"]
RUN dotnet restore "Tech_Store.csproj"

# Copy the rest of the application
COPY . .
WORKDIR "/src/"
RUN dotnet build "Tech_Store.csproj" -c Release -o /app/build
RUN dotnet publish "Tech_Store.csproj" -c Release -o /app/publish

# Final runtime image
FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Tech_Store.dll"]
