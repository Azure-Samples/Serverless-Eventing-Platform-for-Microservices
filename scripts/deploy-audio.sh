#!/bin/bash

HOME=`pwd`

C="\033[1;35m"
D=`date +%Y-%m-%d-%H:%M:%S`
NC='\033[0m'

# Audio Microservice Deploy

echo -e "$C$D Creating audio resource group for $1.$NC"
time az group create -n $1"-audio" -l westus2
echo -e "$C$D Created audio resource group for $1.$NC"

echo -e "$C$D Executing audio deployment for $1.$NC"
time az group deployment create -g $1"-audio" --template-file $HOME/audio/deploy/microservice.json --parameters uniqueResourceNamePrefix=$1 --mode Complete
echo -e "$C$D Executed audio deployment for $1.$NC"

AUDIO_API_NAME=$1"-audio-api"
AUDIO_WORKER_API_NAME=$1"-audio-worker"
AUDIO_BLOB_STORAGE_ACCOUNT_NAME=$1"audioblob"

echo -e "$C$D Creating audio blob storage for $1.$NC"
time az storage container create --account-name $AUDIO_BLOB_STORAGE_ACCOUNT_NAME --name audio
echo -e "$C$D Created audio blob storage for $1.$NC"

echo -e "$C$D Creating audio CORS policy for blob storage for $1.$NC"
time az storage cors clear --account-name $AUDIO_BLOB_STORAGE_ACCOUNT_NAME --services b
time az storage cors add --account-name $AUDIO_BLOB_STORAGE_ACCOUNT_NAME --services b --methods POST GET PUT --origins "*" --allowed-headers "*" --exposed-headers "*"
echo -e "$C$D Created audio CORS policy for blob storage for $1.$NC"

echo -e "$C$D Deploying audio api function for $1.$NC"
time az webapp deployment source config-zip --resource-group $1"-audio" --name $AUDIO_API_NAME --src $HOME/audio/src/ContentReactor.Audio/ContentReactor.Audio.Api/bin/Release/netstandard2.0/ContentReactor.Audio.Api.zip
echo -e "$C$D Deployed audio api function for $1.$NC"
time sleep 3

echo -e "$C$D Deploying audio worker function for $1.$NC"
time az webapp deployment source config-zip --resource-group $1"-audio" --name $AUDIO_WORKER_API_NAME --src $HOME/audio/src/ContentReactor.Audio/ContentReactor.Audio.WorkerApi/bin/Release/netstandard2.0/ContentReactor.Audio.WorkerApi.zip
echo -e "$C$D Deployed audio worker function for $1.$NC"

echo -e "$C$D Deploying audio event grid subscription for $1.$NC"
time az group deployment create -g $1"-events" --template-file $HOME/audio/deploy/eventGridSubscriptions-audio.json --parameters uniqueResourceNamePrefix=$1
echo -e "$C$D Deployed audio event grid subscription for $1.$NC"

time sleep 5
			
echo -e "$C$D Completed audio deployment for $1.$NC"
