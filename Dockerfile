FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["Trailblazers.Backend.csproj", "./"]
RUN dotnet restore "./Trailblazers.Backend.csproj"
COPY . .
RUN dotnet publish "Trailblazers.Backend.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS final
WORKDIR /app
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Trailblazers.Backend.dll"]
