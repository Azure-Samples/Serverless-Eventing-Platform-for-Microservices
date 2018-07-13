#!/bin/bash

HOME=`pwd`

C="\033[1;35m"
D=`date +%Y-%m-%d-%H:%M:%S`
NC='\033[0m'

# Deploy Proxy
echo -e "$C$D Creating the api proxy resource group for $1.$NC"
time az group create -n $1"-proxy" -l westus2
echo -e "$C$D Created the api proxy resource group for $1.$NC"

echo -e "$C$D Executing api proxy deployment for $1.$NC"
time az group deployment create -g $1"-proxy" --template-file $HOME/proxy/deploy/template.json --parameters uniqueResourceNamePrefix=$1 --mode Complete
echo -e "$C$D Executed api proxy deployment for $1.$NC"

echo -e "$C$D Deploying proxy function app for $1.$NC"
PROXY_API_NAME=$1"-proxy-api"
time az webapp deployment source config-zip --resource-group $1"-proxy" --name $PROXY_API_NAME --src $HOME/proxy/proxies/proxies.zip
echo -e "$C$D Deployed proxy function app for $1.$NC"

echo -e "$C$D Completed proxy deployment for $1.$NC"
