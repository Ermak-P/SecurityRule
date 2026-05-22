# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/SecurityRule.Web/SecurityRule.Web.csproj", "SecurityRule.Web/"]
COPY ["src/SecurityRule.Domain/SecurityRule.Domain.csproj", "SecurityRule.Domain/"]
COPY ["src/SecurityRule.Infrastructure/SecurityRule.Infrastructure.csproj", "SecurityRule.Infrastructure/"]
RUN dotnet restore "SecurityRule.Web/SecurityRule.Web.csproj"

COPY src/SecurityRule.Web/ SecurityRule.Web/
COPY src/SecurityRule.Domain/ SecurityRule.Domain/
COPY src/SecurityRule.Infrastructure/ SecurityRule.Infrastructure/
RUN dotnet publish "SecurityRule.Web/SecurityRule.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "SecurityRule.Web.dll"]
