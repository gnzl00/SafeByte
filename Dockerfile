# Multi-stage Docker build for ASP.NET Core 8 backend
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["SafeByte.csproj", "./"]
RUN dotnet restore "SafeByte.csproj"

COPY . .
RUN dotnet publish "SafeByte.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Container default; cloud providers can override with PORT.
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
EXPOSE 8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "SafeByte.dll"]
