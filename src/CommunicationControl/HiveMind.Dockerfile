# https://hub.docker.com/_/microsoft-dotnet
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /source

COPY . .

RUN dotnet restore "DevOpsProject.HiveMind.API/DevOpsProject.HiveMind.API.csproj"
RUN dotnet publish "DevOpsProject.HiveMind.API/DevOpsProject.HiveMind.API.csproj" -c Release -o /app --no-restore


FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app ./
EXPOSE 8080
ENTRYPOINT ["dotnet", "DevOpsProject.HiveMind.API.dll"]