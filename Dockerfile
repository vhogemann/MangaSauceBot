#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/runtime:3.1 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:3.1 AS build
WORKDIR /src
COPY ["MangaSauceBot/MangaSauceBot.csproj", "MangaSauceBot/"]
RUN dotnet restore "MangaSauceBot/MangaSauceBot.csproj"
COPY . .
WORKDIR "/src/MangaSauceBot"
RUN dotnet build "MangaSauceBot.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "MangaSauceBot.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

ARG TWITTER_CONSUMER_SECRET
ENV TWITTER_CONSUMER_SECRET=$TWITTER_CONSUMER_SECRET

ARG TWITTER_CONSUMER_KEY
ENV TWITTER_CONSUMER_KEY=$TWITTER_CONSUMER_KEY

ARG TWITTER_TOKEN_SECRET
ENV TWITTER_TOKEN_SECRET=$TWITTER_TOKEN_SECRET

ARG TWITTER_TOKEN
ENV TWITTER_TOKEN=$TWITTER_TOKEN

ARG BOT_RUN_ONCE=false
ENV BOT_RUN_ONCE=$BOT_RUN_ONCE

ARG BOT_SLEEP_TIMEOUT=600000
ENV BOT_SLEEP_TIMEOUT=$BOT_SLEEP_TIMEOUT

ARG SEARCH_SIMILARITY_CUTOFF=75
ENV SEARCH_SIMILARITY_CUTOFF=$SEARCH_SIMILARITY_CUTOFF

ARG TRACE_MOE_API_KEY
ENV TRACE_MOE_API_KEY=$TRACE_MOE_API_KEY

ARG USE_SQLITE
ENV USE_SQLITE=$USE_SQLITE

ARG COSMOS_DB_ACCOUNT_ENDPOINT
ENV COSMOS_DB_ACCOUNT_ENDPOINT=$COSMOS_DB_ACCOUNT_ENDPOINT

ARG COSMOS_DB_ACCOUNT_KEY
ENV COSMOS_DB_ACCOUNT_KEY=$COSMOS_DB_ACCOUNT_KEY

ARG COSMOS_DB_DATABASE_NAME
ENV COSMOS_DB_DATABASE_NAME=$COSMOS_DB_DATABASE_NAME

ENTRYPOINT ["dotnet", "MangaSauceBot.dll"]