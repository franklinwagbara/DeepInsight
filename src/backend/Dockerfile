FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution and project files
COPY DeepInsight.sln .
COPY DeepInsight.Core/DeepInsight.Core.csproj DeepInsight.Core/
COPY DeepInsight.Infrastructure/DeepInsight.Infrastructure.csproj DeepInsight.Infrastructure/
COPY DeepInsight.Processing/DeepInsight.Processing.csproj DeepInsight.Processing/
COPY DeepInsight.Api/DeepInsight.Api.csproj DeepInsight.Api/

# Restore
RUN dotnet restore

# Copy everything and build
COPY . .
RUN dotnet publish DeepInsight.Api/DeepInsight.Api.csproj -c Release -o /app/publish --no-restore

# Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "DeepInsight.Api.dll"]
