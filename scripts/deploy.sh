#!/bin/bash

echo "Provide Service Principal App ID: "
read servicePrincipalAppId

echo "Provide Service Principal Password: "
read servicePrincipalPassword

echo "Provide Service Principal Tenant ID: "
read servicePrincipalTenantId

echo "Provide subscription ID: "
read subscriptionId

HOME=`pwd`
echo "Provide any unique Prefix string (max length 15 characters, recommended to autogenerate a string): "
read uniquePrefixString

echo "Provide Big Huge Thesaurus API Key: "
read bigHugeThesaurusApiKey

az login --service-principal --username $servicePrincipalAppId --password $servicePrincipalPassword --tenant $servicePrincipalTenantId
az account set --subscription $subscriptionId 

# Creating Event Grid Topic

echo "Creating Event Grid Topic..."
az group create -n $uniquePrefixString"-events" -l westus2
EVENT_GRID_TOPIC_NAME=$uniquePrefixString"-events-topic"
az group deployment create -g $uniquePrefixString"-events" --template-file $HOME/events/deploy/template.json --mode Complete --parameters uniqueResourceNamePrefix=$uniquePrefixString
sleep 2
# Categories Microservice Deploy

echo "Starting deploy of Categories Microservice..."
az group create -n $uniquePrefixString"-categories" -l westus2
az group deployment create -g $uniquePrefixString"-categories" --template-file $HOME/categories/deploy/microservice.json --parameters uniqueResourceNamePrefix=$uniquePrefixString bigHugeThesaurusApiKey=$bigHugeThesaurusApiKey --mode Complete

echo "Creating Cosmos DB entries..."
COSMOS_DB_ACCOUNT_NAME=$uniquePrefixString"-categories-db"
az cosmosdb database create --name $COSMOS_DB_ACCOUNT_NAME --db-name Categories --resource-group $uniquePrefixString"-categories"
az cosmosdb collection create --name $COSMOS_DB_ACCOUNT_NAME --db-name Categories --collection-name Categories --resource-group $uniquePrefixString"-categories" --partition-key-path "/userId" --throughput 400

CATEGORIES_API_NAME=$uniquePrefixString"-categories-api"
CATEGORIES_WORKER_API_NAME=$uniquePrefixString"-categories-worker"

echo "Deploying Categories Functions..."
az webapp deployment source config-zip --resource-group $uniquePrefixString"-categories" --name $CATEGORIES_API_NAME --src $HOME/categories/src/ContentReactor.Categories/ContentReactor.Categories.Api/bin/Release/netstandard2.0/ContentReactor.Categories.Api.zip
az webapp deployment source config-zip --resource-group $uniquePrefixString"-categories" --name $CATEGORIES_WORKER_API_NAME --src $HOME/categories/src/ContentReactor.Categories/ContentReactor.Categories.WorkerApi/bin/Release/netstandard2.0/ContentReactor.Categories.WorkerApi.zip

echo "Deploying Event Grid Subscription for Categories"
az group deployment create -g $uniquePrefixString"-events" --template-file $HOME/categories/deploy/eventGridSubscriptions.json --parameters uniqueResourceNamePrefix=$uniquePrefixString
sleep 5

# Images Microservice Deploy

echo "Starting deploy of Images Microservice..."
az group create -n $uniquePrefixString"-images" -l westus2
az group deployment create -g $uniquePrefixString"-images" --template-file $HOME/images/deploy/microservice.json --parameters uniqueResourceNamePrefix=$uniquePrefixString --mode Complete

IMAGES_API_NAME=$uniquePrefixString"-images-api"
IMAGES_WORKER_API_NAME=$uniquePrefixString"-images-worker"
sleep 1
echo "Creating Images Blob Storage..."
IMAGES_BLOB_STORAGE_ACCOUNT_NAME=$uniquePrefixString"imagesblob"
az storage container create --account-name $IMAGES_BLOB_STORAGE_ACCOUNT_NAME --name fullimages
az storage container create --account-name $IMAGES_BLOB_STORAGE_ACCOUNT_NAME --name previewimages

echo "Creating CORS Policy for Blob Storage"
az storage cors clear --account-name $IMAGES_BLOB_STORAGE_ACCOUNT_NAME --services b
az storage cors add --account-name $IMAGES_BLOB_STORAGE_ACCOUNT_NAME --services b --methods POST GET PUT --origins "*" --allowed-headers "*" --exposed-headers "*"

echo "Deploying Images Functions..."
az webapp deployment source config-zip --resource-group $uniquePrefixString"-images" --name $IMAGES_API_NAME --src $HOME/images/src/ContentReactor.Images/ContentReactor.Images.Api/bin/Release/netstandard2.0/ContentReactor.Images.Api.zip
sleep 2
az webapp deployment source config-zip --resource-group $uniquePrefixString"-images" --name $IMAGES_WORKER_API_NAME --src $HOME/images/src/ContentReactor.Images/ContentReactor.Images.WorkerApi/bin/Release/netstandard2.0/ContentReactor.Images.WorkerApi.zip

echo "Deploying Event Grid Subscription for Images"
az account set --subscription $subscriptionId
az group deployment create -g $uniquePrefixString"-events" --template-file $HOME/images/deploy/eventGridSubscriptions.json --parameters uniqueResourceNamePrefix=$uniquePrefixString
sleep 5

# Audio Microservice Deploy

echo "Starting deploy of Audio Microservice..."
az group create -n $uniquePrefixString"-audio" -l westus2
az group deployment create -g $uniquePrefixString"-audio" --template-file $HOME/audio/deploy/microservice.json --parameters uniqueResourceNamePrefix=$uniquePrefixString --mode Complete

AUDIO_API_NAME=$uniquePrefixString"-audio-api"
AUDIO_WORKER_API_NAME=$uniquePrefixString"-audio-worker"

echo "Creating Audio Blob Storage..."
AUDIO_BLOB_STORAGE_ACCOUNT_NAME=$uniquePrefixString"audioblob"
az storage container create --account-name $AUDIO_BLOB_STORAGE_ACCOUNT_NAME --name audio

echo "Creating CORS Policy for Blob Storage"
az storage cors clear --account-name $AUDIO_BLOB_STORAGE_ACCOUNT_NAME --services b
az storage cors add --account-name $AUDIO_BLOB_STORAGE_ACCOUNT_NAME --services b --methods POST GET PUT --origins "*" --allowed-headers "*" --exposed-headers "*"

echo "Deploying Audio Functions..."
az webapp deployment source config-zip --resource-group $uniquePrefixString"-audio" --name $AUDIO_API_NAME --src $HOME/audio/src/ContentReactor.Audio/ContentReactor.Audio.Api/bin/Release/netstandard2.0/ContentReactor.Audio.Api.zip
sleep 3
az webapp deployment source config-zip --resource-group $uniquePrefixString"-audio" --name $AUDIO_WORKER_API_NAME --src $HOME/audio/src/ContentReactor.Audio/ContentReactor.Audio.WorkerApi/bin/Release/netstandard2.0/ContentReactor.Audio.WorkerApi.zip

echo "Deploying Event Grid Subscription for Audio"
az group deployment create -g $uniquePrefixString"-events" --template-file $HOME/audio/deploy/eventGridSubscriptions.json --parameters uniqueResourceNamePrefix=$uniquePrefixString
sleep 5

# Text Microservice Deploy

echo "Starting deploy of Text Microservice..."
az group create -n $uniquePrefixString"-text" -l westus2
az group deployment create -g $uniquePrefixString"-text" --template-file $HOME/text/deploy/microservice.json --parameters uniqueResourceNamePrefix=$uniquePrefixString --mode Complete

echo "Creating Text Cosmos DB..."
TEXT_COSMOSDB_STORAGE_ACCOUNT_NAME=$uniquePrefixString"-text-db"
az cosmosdb database create --name $TEXT_COSMOSDB_STORAGE_ACCOUNT_NAME --db-name Text --resource-group $uniquePrefixString"-text"
az cosmosdb collection create --name $TEXT_COSMOSDB_STORAGE_ACCOUNT_NAME --db-name Text --collection-name Text --resource-group $uniquePrefixString"-text" --partition-key-path "/userId" --throughput 400

echo "Deploying Text Functions..."
TEXT_API_NAME=$uniquePrefixString"-text-api"
az webapp deployment source config-zip --resource-group $uniquePrefixString"-text" --name $TEXT_API_NAME --src $HOME/text/src/ContentReactor.Text/ContentReactor.Text.Api/bin/Release/netstandard2.0/ContentReactor.Text.Api.zip

# Deploy Proxy
echo "Starting deploy of Proxy..."
az group create -n $uniquePrefixString"-proxy" -l westus2
az group deployment create -g $uniquePrefixString"-proxy" --template-file $HOME/proxy/deploy/template.json --parameters uniqueResourceNamePrefix=$uniquePrefixString --mode Complete
PROXY_API_NAME=$uniquePrefixString"-proxy-api"
az webapp deployment source config-zip --resource-group $uniquePrefixString"-proxy" --name $PROXY_API_NAME --src $HOME/proxy/proxies/proxies.zip

# Deploy Web
echo "Starting deploy of Web..."
az group create -n $uniquePrefixString"-web" -l westus2

az group deployment create --name $uniquePrefixString"-web-deployment" --resource-group $uniquePrefixString"-web" --template-file $HOME/web/deploy/template.json --parameters uniqueResourceNamePrefix=$uniquePrefixString
WEB_APP_NAME=$uniquePrefixString"-web-app"
webInstrumentationKey=$(az resource show --namespace microsoft.insights --resource-type components --name $uniquePrefixString-monitor-ai -g $uniquePrefixString-monitor --query properties.InstrumentationKey)
sed -i -e 's/\"%INSTRUMENTATION_KEY%\"/'"$webInstrumentationKey"'/g' $HOME/web/src/signalr-web/SignalRMiddleware/EventApp/src/environments/environment.ts
sed -i -e 's/\"%INSTRUMENTATION_KEY%\"/'"$webInstrumentationKey"'/g' $HOME/web/src/signalr-web/SignalRMiddleware/EventApp/src/environments/environment.prod.ts

cd $HOME/web/src/signalr-web/SignalRMiddleware/EventApp
npm install
npm run ubuntu-dev-build

cd $HOME/web/src/signalr-web/SignalRMiddleware/SignalRMiddleware
dotnet publish -c Release

cd $HOME/web/src/signalr-web/SignalRMiddleware/SignalRMiddleware/bin/Release/netcoreapp2.1/publish/
zip -r SignalRMiddleware.zip .

az webapp deployment source config-zip --resource-group $uniquePrefixString"-web" --name $WEB_APP_NAME --src $HOME/web/src/signalr-web/SignalRMiddleware/SignalRMiddleware/bin/Release/netcoreapp2.1/publish/SignalRMiddleware.zip
az group deployment create -g $uniquePrefixString"-events" --template-file $HOME/web/deploy/eventGridSubscriptions.json --parameters uniqueResourceNamePrefix=$uniquePrefixString
