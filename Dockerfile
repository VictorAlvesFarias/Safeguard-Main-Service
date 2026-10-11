# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src

# Project files first: the restore layer is reused while only the code changes.
COPY ["App/App.csproj", "App/"]
COPY ["Application/Application.csproj", "Application/"]
COPY ["Domain/Domain.csproj", "Domain/"]
COPY ["Infrastructure/Infrastructure.csproj", "Infrastructure/"]
RUN dotnet restore "App/App.csproj"

COPY . .
RUN dotnet publish "App/App.csproj" -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS final
WORKDIR /app

# Plain HTTP on every interface: TLS ends outside the container (see Program.cs, Ports:ListenAnyIp).
ENV ASPNETCORE_ENVIRONMENT=Production \
    Ports__ListenAnyIp=true \
    Ports__Http=8080
EXPOSE 8080

RUN groupadd --system app && useradd --system --gid app --no-create-home app
COPY --from=build /app/publish .
USER app

ENTRYPOINT ["dotnet", "App.dll"]
