#!/bin/bash

HOME=`pwd`

# Text Microservice Deploy

C="\033[1;35m"
D=`date +%Y-%m-%d-%H:%M:%S`
NC='\033[0m'

echo -e "$C$D Creating text resource group for $1.$NC"
time az group create -n $1"-text" -l westus2
echo -e "$C$D Created text resource group for $1.$NC"

echo -e "$C$D Executing text deployment for $1.$NC"
time az group deployment create -g $1"-text" --template-file $HOME/text/deploy/microservice.json --parameters uniqueResourceNamePrefix=$1 --mode Complete
echo -e "$C$D Executed text deployment for $1.$NC"

echo -e "$C$D Creating text cosmos db for $1.$NC"
TEXT_COSMOSDB_STORAGE_ACCOUNT_NAME=$1"-text-db"
time az cosmosdb database create --name $TEXT_COSMOSDB_STORAGE_ACCOUNT_NAME --db-name Text --resource-group $1"-text"
echo -e "$C$D Created text cosmos db for $1.$NC"

echo -e "$C$D Creating text cosmos collection for $1.$NC"
time az cosmosdb collection create --name $TEXT_COSMOSDB_STORAGE_ACCOUNT_NAME --db-name Text --collection-name Text --resource-group $1"-text" --partition-key-path "/userId" --throughput 400
echo -e "$C$D Created text cosmos collection for $1.$NC"

echo -e "$C$D Deploying text api functions for $1.$NC"
TEXT_API_NAME=$1"-text-api"
time az webapp deployment source config-zip --resource-group $1"-text" --name $TEXT_API_NAME --src $HOME/text/src/ContentReactor.Text/ContentReactor.Text.Api/bin/Release/netstandard2.0/ContentReactor.Text.Api.zip
echo -e "$C$D Deployed text api functions for $1.$NC"

echo -e "$C$D Completed text deployment for $1.$NC"
