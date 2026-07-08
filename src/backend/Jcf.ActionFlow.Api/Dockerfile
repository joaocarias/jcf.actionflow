FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Jcf.ActionFlow.slnx ./
COPY Jcf.ActionFlow.Core/Jcf.ActionFlow.Core.csproj Jcf.ActionFlow.Core/
COPY Jcf.ActionFlow.Api/Jcf.ActionFlow.Api.csproj Jcf.ActionFlow.Api/
RUN dotnet restore Jcf.ActionFlow.Api/Jcf.ActionFlow.Api.csproj

COPY Jcf.ActionFlow.Core/ Jcf.ActionFlow.Core/
COPY Jcf.ActionFlow.Api/ Jcf.ActionFlow.Api/
RUN dotnet publish Jcf.ActionFlow.Api/Jcf.ActionFlow.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app .
EXPOSE 8080
ENTRYPOINT ["dotnet", "Jcf.ActionFlow.Api.dll"]
