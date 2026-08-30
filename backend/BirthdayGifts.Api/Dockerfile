FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY BirthdayGifts.Api.csproj ./
RUN dotnet restore
COPY . ./
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine
WORKDIR /app
RUN apk add --no-cache wget
COPY --from=build /app/publish ./
RUN mkdir -p /app/uploads
ENTRYPOINT ["dotnet", "BirthdayGifts.Api.dll"]
