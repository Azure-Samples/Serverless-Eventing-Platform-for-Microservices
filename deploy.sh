 #!/bin/bash
echo "Provide Service Principal App ID: "
read servicePrincipalAppId

echo "Provide Service Principal Password: "
read servicePrincipalPassword

echo "Provide Service Principal Tenant ID: "
read servicePrincipalTenantId

az login --service-principal --username $servicePrincipalAppId --password $servicePrincipalPassword --tenant $servicePrincipalTenantId

echo "Provide subscription ID: "
read subscriptionId
az account set --subscription $subscriptionId 

#az group create -n ContentReactor-Audio -l westus2
#az group create -n ContentReactor-Categories -l westus2
#az group create -n ContentReactor-Events -l westus2
#az group create -n ContentReactor-Images -l westus2
#az group create -n ContentReactor-Proxy -l westus2
#az group create -n ContentReactor-Text -l westus2
#az group create -n ContentReactor-Web -l westus2

HOME=`pwd`
echo "Provide an unique suffix string (recommended to autogenerate string to guarantee uniqueness): "
read uniqueSuffixString

echo "Provide Big Huge Thesaurus API Key: "
read bigHugeThesaurusApiKey

# Creating Event Grid Topic

echo "Creating Event Grid Topic..."
az account set --subscription $subscriptionId 
az group create -n ContentReactor-Events -l westus2
EVENT_GRID_TOPIC_NAME=contentreactor$uniqueSuffixString
az group deployment create -g ContentReactor-Events --template-file $HOME/events/deploy/template.json --mode Complete --parameters uniqueResourceNameSuffix=$uniqueSuffixString

# Categories Microservice Deploy

echo "Starting deploy of Categories Microservice..."
az account set --subscription $subscriptionId 
az group create -n ContentReactor-Categories -l westus2
az group deployment create -g ContentReactor-Categories --template-file $HOME/categories/deploy/microservice.json --parameters uniqueResourceNameSuffix=$uniqueSuffixString eventsResourceGroupName=ContentReactor-Events eventGridTopicName=$EVENT_GRID_TOPIC_NAME bigHugeThesaurusApiKey=$bigHugeThesaurusApiKey --mode Complete

CATEGORIES_API_NAME=crcatapi$uniqueSuffixString
CATEGORIES_WORKER_API_NAME=crcatwapi$uniqueSuffixString

echo "Creating Cosmos DB entries..."
az account set --subscription $subscriptionId 
COSMOS_DB_ACCOUNT_NAME=crcatdb$uniqueSuffixString
az cosmosdb database create --name $COSMOS_DB_ACCOUNT_NAME --db-name Categories --resource-group ContentReactor-Categories
az cosmosdb collection create --name $COSMOS_DB_ACCOUNT_NAME --db-name Categories --collection-name Categories --resource-group ContentReactor-Categories --partition-key-path "/userId" --throughput 1000

echo "Deploying Categories Functions..."
az account set --subscription $subscriptionId 
az webapp deployment source config-zip --resource-group ContentReactor-Categories --name $CATEGORIES_API_NAME --src $HOME/categories/src/ContentReactor.Categories/ContentReactor.Categories.Api/bin/Release/netstandard2.0/ContentReactor.Categories.Api.zip
az webapp deployment source config-zip --resource-group ContentReactor-Categories --name $CATEGORIES_WORKER_API_NAME --src $HOME/categories/src/ContentReactor.Categories/ContentReactor.Categories.WorkerApi/bin/Release/netstandard2.0/ContentReactor.Categories.WorkerApi.zip

echo "Deploying Event Grid Subscription for Categories"
az account set --subscription $subscriptionId
az group deployment create -g ContentReactor-Events --template-file $HOME/categories/deploy/eventGridSubscriptions.json --parameters eventGridTopicName=$EVENT_GRID_TOPIC_NAME microserviceResourceGroupName=ContentReactor-Categories microserviceFunctionsWorkerApiAppName=$CATEGORIES_WORKER_API_NAME

# Images Microservice Deploy

echo "Starting deploy of Images Microservice..."
az account set --subscription $subscriptionId 
az group create -n ContentReactor-Images -l westus2
az group deployment create -g ContentReactor-Images --template-file $HOME/images/deploy/microservice.json --parameters uniqueResourceNameSuffix=$uniqueSuffixString eventGridTopicName=$EVENT_GRID_TOPIC_NAME --mode Complete

IMAGES_API_NAME=crimgapi$uniqueSuffixString
IMAGES_WORKER_API_NAME=crimgwapi$uniqueSuffixString

echo "Creating Images Blob Storage..."
az account set --subscription $subscriptionId 
IMAGES_BLOB_STORAGE_ACCOUNT_NAME=crimagesblob$uniqueSuffixString
az storage container create --account-name $IMAGES_BLOB_STORAGE_ACCOUNT_NAME --name fullimages
az storage container create --account-name $IMAGES_BLOB_STORAGE_ACCOUNT_NAME --name previewimages

echo "Creating CORS Policy for Blob Storage"
az account set --subscription $subscriptionId 
az storage cors clear --account-name $IMAGES_BLOB_STORAGE_ACCOUNT_NAME
az storage cors add --account-name $IMAGES_BLOB_STORAGE_ACCOUNT_NAME --services b --methods POST GET PUT --origins * --allowed-headers * --exposed-headers *

echo "Deploying Images Functions..."
az account set --subscription $subscriptionId 
az webapp deployment source config-zip --resource-group ContentReactor-Images --name  $IMAGES_API_NAME --src $HOME/images/src/ContentReactor.Images/ContentReactor.Images.Api/bin/Release/netstandard2.0/ContentReactor.Images.Api.zip
az webapp deployment source config-zip --resource-group ContentReactor-Images --name $IMAGES_WORKER_API_NAME --src $HOME/images/src/ContentReactor.Images/ContentReactor.Images.WorkerApi/bin/Release/netstandard2.0/ContentReactor.Images.WorkerApi.zip

echo "Deploying Event Grid Subscription for Images"
az account set --subscription $subscriptionId
az group deployment create -g ContentReactor-Events --template-file $HOME/images/deploy/eventGridSubscriptions.json --parameters eventGridTopicName=$EVENT_GRID_TOPIC_NAME microserviceResourceGroupName=ContentReactor-Images microserviceFunctionsWorkerApiAppName=$IMAGES_WORKER_API_NAME

# Audio Microservice Deploy

echo "Starting deploy of Audio Microservice..."
az account set --subscription $subscriptionId 
az group create -n ContentReactor-Audio -l westus2
az group deployment create -g ContentReactor-Audio --template-file $HOME/audio/deploy/microservice.json --parameters uniqueResourceNameSuffix=$uniqueSuffixString eventGridTopicName=$EVENT_GRID_TOPIC_NAME --mode Complete

AUDIO_API_NAME=craudapi$uniqueSuffixString
AUDIO_WORKER_API_NAME=craudwapi$uniqueSuffixString

echo "Creating Audio Blob Storage..."
az account set --subscription $subscriptionId 
AUDIO_BLOB_STORAGE_ACCOUNT_NAME=craudioblob$uniqueSuffixString
az storage container create --account-name $AUDIO_BLOB_STORAGE_ACCOUNT_NAME --name audio

echo "Creating CORS Policy for Blob Storage"
az account set --subscription $subscriptionId 
az storage cors clear --account-name $AUDIO_BLOB_STORAGE_ACCOUNT_NAME
az storage cors add --account-name $AUDIO_BLOB_STORAGE_ACCOUNT_NAME --services b --methods POST GET PUT --origins * --allowed-headers * --exposed-headers *

echo "Deploying Audio Functions..."
az account set --subscription $subscriptionId 
az webapp deployment source config-zip --resource-group ContentReactor-Audio --name $AUDIO_API_NAME --src $HOME/audio/src/ContentReactor.Audio/ContentReactor.Audio.Api/bin/Release/netstandard2.0/ContentReactor.Audio.Api.zip
az webapp deployment source config-zip --resource-group ContentReactor-Audio --name $AUDIO_WORKER_API_NAME --src $HOME/audio/src/ContentReactor.Audio/ContentReactor.Audio.WorkerApi/bin/Release/netstandard2.0/ContentReactor.Audio.WorkerApi.zip

echo "Deploying Event Grid Subscription for Audio"
az account set --subscription $subscriptionId
az group deployment create -g ContentReactor-Events --template-file $HOME/audio/deploy/eventGridSubscriptions.json --parameters eventGridTopicName=$EVENT_GRID_TOPIC_NAME microserviceResourceGroupName=ContentReactor-Audio microserviceFunctionsWorkerApiAppName=$AUDIO_WORKER_API_NAME

# Text Microservice Deploy

echo "Starting deploy of Text Microservice..."
az account set --subscription $subscriptionId 
az group deployment create -g ContentReactor-Text --template-file $HOME/text/deploy/microservice.json --parameters eventGridTopicName=$EVENT_GRID_TOPIC_NAME --mode Complete

echo "Creating Text Blob Storage..."
TEXT_BLOB_STORAGE_ACCOUNT_NAME=crtextdb$uniqueSuffixString
az account set --subscription $subscriptionId 
az cosmosdb database create --name $TEXT_BLOB_STORAGE_ACCOUNT_NAME --db-name Text --resource-group ContentReactor-Text
az cosmosdb collection create --name $TEXT_BLOB_STORAGE_ACCOUNT_NAME --db-name Text --collection-name Text --resource-group ContentReactor-Text --partition-key-path "/userId" --throughput 1000

echo "Deploying Text Functions..."
az account set --subscription $subscriptionId 
TEXT_API_NAME=crtxtapi$uniqueSuffixString
az webapp deployment source config-zip --resource-group ContentReactor-Text --name $TEXT_API_NAME --src $HOME/text/src/ContentReactor.Text/ContentReactor.Text.Api/bin/Release/netstandard2.0/ContentReactor.Text.Api.zip

# Deploy Proxy
echo "Starting deploy of Proxy..."
az account set --subscription $subscriptionId 
az group deployment create -g ContentReactor-Proxy --template-file $HOME/proxy/deploy/template.json --parameters categoriesMicroserviceApiAppName=$CATEGORIES_API_NAME imagesMicroserviceApiAppName=$IMAGES_API_NAME audioMicroserviceApiAppName=$AUDIO_API_NAME textMicroserviceApiAppName=$TEXT_API_NAME --mode Complete
PROXY_API_NAME=crapiproxy$uniqueSuffixString
az webapp deployment source config-zip --resource-group ContentReactor-Proxies --name $PROXY_API_NAME --src $HOME/proxy/proxies/proxies.zip

# Deploy Web 
echo "Starting deploy of Web..."
az account set --subscription $subscriptionId 
az group deployment create --name ContentReactorWeb-Deployment --resource-group ContentReactor-Web --template-file $HOME/web/deploy/template.json --parameters uniqueResourceNameSuffix=$uniqueSuffixString functionAppProxyName=crapiproxy$uniqueSuffixString
WEB_APP_NAME=crweb$uniqueSuffixString
az webapp deployment source config-zip --resource-group ContentReactor-Web --name $WEB_APP_NAME --src $HOME/web/src/signalr-web/SignalRMiddleware/SignalRMiddleware/obj/Release/netcoreapp2.1/SignalRMiddleware.dll
