FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["NloSurpriseCalendar.slnx", "./"]
COPY ["src/NloSurpriseCalendar.App/NloSurpriseCalendar.App.csproj", "src/NloSurpriseCalendar.App/"]
RUN dotnet restore "src/NloSurpriseCalendar.App/NloSurpriseCalendar.App.csproj"

COPY . .
RUN dotnet publish "src/NloSurpriseCalendar.App/NloSurpriseCalendar.App.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
RUN apt-get update \
    && apt-get install -y --no-install-recommends libgssapi-krb5-2 \
    && rm -rf /var/lib/apt/lists/*
COPY --from=build /app/publish .

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "NloSurpriseCalendar.App.dll"]
