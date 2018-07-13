#!/bin/bash

HOME=`pwd`

C="\033[1;35m"
D=`date +%Y-%m-%d-%H:%M:%S`
NC='\033[0m'

# Categories Microservice Deploy

echo -e "$C$D Creating the categories resource group for $1.$NC"
time az group create -n $1"-categories" -l westus2
echo -e "$C$D Created the categories resource group for $1.$NC"

echo -e "$C$D Executing the categories deployment for $1.$NC"
time az group deployment create -g $1"-categories" --template-file $HOME/categories/deploy/microservice.json --parameters uniqueResourceNamePrefix=$1 bigHugeThesaurusApiKey=$2 --mode Complete
echo -e "$C$D Executed the categories deployment for $1.$NC"

echo -e "$C$D Creating categories cosmos db for $1.$NC"
COSMOS_DB_ACCOUNT_NAME=$1"-categories-db"
time az cosmosdb database create --name $COSMOS_DB_ACCOUNT_NAME --db-name Categories --resource-group $1"-categories"
echo -e "$C$D Created categories cosmos db for $1.$NC"

echo -e "$C$D Creating categories cosmos db collection for $1.$NC"
time az cosmosdb collection create --name $COSMOS_DB_ACCOUNT_NAME --db-name Categories --collection-name Categories --resource-group $1"-categories" --partition-key-path "/userId" --throughput 400
echo -e "$C$D Created categories cosmos db collection for $1.$NC"

CATEGORIES_API_NAME=$1"-categories-api"
CATEGORIES_WORKER_API_NAME=$1"-categories-worker"

echo -e "$C$D Deploying categories api functions for $1.$NC"
time az webapp deployment source config-zip --resource-group $1"-categories" --name $CATEGORIES_API_NAME --src $HOME/categories/src/ContentReactor.Categories/ContentReactor.Categories.Api/bin/Release/netstandard2.0/ContentReactor.Categories.Api.zip
echo -e "$C$D Deployed categories api functions for $1.$NC"

echo -e "$C$D Deploying categories worker functions for $1.$NC"
time az webapp deployment source config-zip --resource-group $1"-categories" --name $CATEGORIES_WORKER_API_NAME --src $HOME/categories/src/ContentReactor.Categories/ContentReactor.Categories.WorkerApi/bin/Release/netstandard2.0/ContentReactor.Categories.WorkerApi.zip
echo -e "$C$D Deployed categories worker functions for $1.$NC"

echo -e "$C$D Deploying categories event grid subscription for $1.$NC"
time az group deployment create -g $1"-events" --template-file $HOME/categories/deploy/eventGridSubscriptions-categories.json --parameters uniqueResourceNamePrefix=$1
echo -e "$C$D Deployed categories event grid subscription for $1.$NC"
time sleep 5

echo -e "$C$D Completed categories deployment. for $1.$NC"
