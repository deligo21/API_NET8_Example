FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

COPY . .
RUN dotnet restore
RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/out .

ENV ASPNETCORE_URLS=https://+:5001;http://+:5000
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 5000 5001

ENTRYPOINT ["dotnet", "PaymentAPI.dll"]
