# Stage 1: Build React frontend
FROM node:20-alpine AS frontend-build
WORKDIR /app/frontend
COPY frontend/package.json frontend/package-lock.json ./
RUN npm ci
COPY frontend/ ./
RUN npm run build

# Stage 2: Build ASP.NET API
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS api-build
WORKDIR /app
COPY PersonnelOrg.slnx ./
COPY src/Api/Api.csproj ./src/Api/
RUN dotnet restore src/Api/Api.csproj
COPY src/ ./src/
RUN dotnet publish src/Api/Api.csproj -c Release -o /app/publish --no-restore

# Stage 3: Final runtime image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=api-build /app/publish ./
COPY --from=frontend-build /app/frontend/dist ./wwwroot/

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Api.dll"]
