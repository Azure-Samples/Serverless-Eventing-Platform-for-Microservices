#!/bin/bash

HOME=`pwd`

C="\033[1;35m"
D=`date +%Y-%m-%d-%H:%M:%S`
NC='\033[0m'

# Images Microservice Deploy

echo -e "$C$D Creating images resource group for $1.$NC"
time az group create -n $1"-images" -l westus2
echo -e "$C$D Created images resource group for $1.$NC"

echo -e "$C$D Executing images deployment for $1.$NC"
time az group deployment create -g $1"-images" --template-file $HOME/images/deploy/microservice.json --parameters uniqueResourceNamePrefix=$1 --mode Complete
echo -e "$C$D Executed images deployment for $1.$NC"

IMAGES_API_NAME=$1"-images-api"
IMAGES_WORKER_API_NAME=$1"-images-worker"
time sleep 1
IMAGES_BLOB_STORAGE_ACCOUNT_NAME=$1"imagesblob"

echo -e "$C$D Creating images blob storage for $1.$NC"
time az storage container create --account-name $IMAGES_BLOB_STORAGE_ACCOUNT_NAME --name fullimages
time az storage container create --account-name $IMAGES_BLOB_STORAGE_ACCOUNT_NAME --name previewimages
echo -e "$C$D Created images blob storage for $1.$NC"

echo -e "$C$D Creating images blob CORS policy for $1.$NC"
time az storage cors clear --account-name $IMAGES_BLOB_STORAGE_ACCOUNT_NAME --services b
time az storage cors add --account-name $IMAGES_BLOB_STORAGE_ACCOUNT_NAME --services b --methods POST GET PUT --origins "*" --allowed-headers "*" --exposed-headers "*"
echo -e "$C$D Created images blob CORS policy for $1.$NC"

echo -e "$C$D Deploying images api functions for $1.$NC"
time az webapp deployment source config-zip --resource-group $1"-images" --name $IMAGES_API_NAME --src $HOME/images/src/ContentReactor.Images/ContentReactor.Images.Api/bin/Release/netstandard2.0/ContentReactor.Images.Api.zip
echo -e "$C$D Deployed images api functions for $1.$NC"

time sleep 2

echo -e "$C$D Deploying images worker functions for $1.$NC"
time az webapp deployment source config-zip --resource-group $1"-images" --name $IMAGES_WORKER_API_NAME --src $HOME/images/src/ContentReactor.Images/ContentReactor.Images.WorkerApi/bin/Release/netstandard2.0/ContentReactor.Images.WorkerApi.zip
echo -e "$C$D Deployed images worker functions for $1.$NC"

echo -e "$C$D Deploying images event grid subscription for $1.$NC"
time az group deployment create -g $1"-events" --template-file $HOME/images/deploy/eventGridSubscriptions-images.json --parameters uniqueResourceNamePrefix=$1
echo -e "$C$D Deployed images event grid subscription for $1.$NC"

time sleep 5

echo -e "$C$D Completed  deployment for $1.$NC"
