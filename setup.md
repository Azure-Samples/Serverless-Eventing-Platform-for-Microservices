# Serverless Eventing Platform for Microservices

## Deploying Content Reactor

Content Reactor can be built and deployed into your own Azure subscription. You can use Azure Pipelines to run builds and releases, or if you prefer, you can run the build and deployment steps manually. We assume that you have an Azure subscription available to run the sample; [you can get a free trial Azure subscription here](https://azure.microsoft.com/en-us/free/).

This guide explains the steps to build and deploy Content Reactor, both using Azure Pipelines and manually. For manual deployments we suggest using the [Azure CLI](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli?view=azure-cli-latest), a cross-platform command-line interface for Azure.

The overall sequence involved in building and deploying Content Reactor is:

 1. **Creating resource groups.**
 2. **Building Content Reactor.**
 3. **Deploying Content Reactor.** Each component and microservice has its own deployment process, which is described in detail below.

## Creating Resource Groups

Each component of Content Reactor - each microservice, the Event Grid topic, the web front-end, and the API proxy - should be deployed into their own resource group. We have used the following resource group names:

* `ContentReactor-Audio`
* `ContentReactor-Categories`
* `ContentReactor-Events`
* `ContentReactor-Images`
* `ContentReactor-Proxy`
* `ContentReactor-Text`
* `ContentReactor-Web`

As this is a one-time activity, you may decide to use the [Azure Portal to create the resource groups](https://docs.microsoft.com/en-us/azure/azure-resource-manager/resource-group-portal#manage-resource-groups). We recommend placing the resources in the `West US 2` region, since it allows for all of the resource types we use. Other regions that support Event Grid ([see a list of these regions here](https://docs.microsoft.com/en-us/azure/event-grid/overview)) may also be used.

Alternatively, you may also prefer to use the Azure CLI to create the resource groups. After [logging into your Azure subscription](https://docs.microsoft.com/en-us/cli/azure/authenticate-azure-cli?view=azure-cli-latest) you can execute the following commands to create the resource groups:

* `az group create -n ContentReactor-Audio -l westus2`
* `az group create -n ContentReactor-Categories -l westus2`
* `az group create -n ContentReactor-Events -l westus2`
* `az group create -n ContentReactor-Images -l westus2`
* `az group create -n ContentReactor-Proxy -l westus2`
* `az group create -n ContentReactor-Text -l westus2`
* `az group create -n ContentReactor-Web -l westus2`

## Building Content Reactor

### Using Azure Pipelines

Each of the subfolders in this repository (`audio`, `categories`, `events`, `images`, `proxy`, `text`, and `web`) contains a `build` subfolder with a `build.yaml` file. The `build.yaml` files contain the list of Azure Pipelines build steps that are required for that component.

To use Azure Pipelines to build the Content Reactor system, you will need to set up multiple build configurations - one for each component with a `build.yaml` file. [Follow the instructions here](https://docs.microsoft.com/en-us/Azure Pipelines/build-release/actions/build-yaml?view=Azure Pipelines#manually-create-a-yaml-build-definition) to create each build definition and select the appropriate `build.yaml` file.

After all the build definitions have been created, queue builds using those definitions

### Manually

Each microservice's folder (`audio`, `categories`, `images`, and `text`) contains a Visual Studio solution within the `src` subfolder. The solutions can be built using Visual Studio 2017 or higher, or by using the `dotnet build` CLI command. The application artifacts can be collected using the `dotnet publish` command. Unit tests can be executed by using the `dotnet test` command. Once a microservice is built, the published API projects should be collected into `.zip` files to prepare for deployment.

For example, to build the categories microservice, you would execute the following commands from within the `categories/src/ContentReactor.Categories` subfolder:

 1. `dotnet build`
 2. `dotnet test`
 3. `dotnet publish -c Release`
 4. Zip the contents of the `ContentReactor.Categories.Api/bin/Release/netstandard2.0` folder and name it `ContentReactor.Categories.Api.zip`.
 5. Zip the contents of the `ContentReactor.Categories.WorkerApi/bin/Release/netstandard2.0` folder and name it `ContentReactor.Categories.WorkerApi.zip`.

## Building Web Manually

The front end and the signalR middleware is in the signalr-web/SignalRMiddleware folder. This folder has a solution file which can be built using Visual Studio Version 15.7.0 and above
The build takes care of building the angular component and bundling this with the ASP.NET WebApp.

Note: Please make sure that your build host or your local machine has the latest versions of node.js and npm installed.

Build the ASP.NET Web Application using the following steps:

1. Build the Angular app:
    Go to web/src/signalr-web/SignalRMiddleware/EventApp and perform `npm install` followed by `npm run dev`

2. Restore NuGet packages:
    `dotnet restore web/src/signalr-web/SignalRMiddleware/SignalRMiddleware.sln`

3. Build MVC App: This will bundle Angular app and ASP.NET app using MSBuild
    `dotnet build  web\src\signalr-web\SignalRMiddleware\SignalRMiddleware.sln`

4. Zip the contents of web\src\signalr-web\SignalRMiddleware\SignalRMiddleware\obj\Release\netcoreapp2.1

## Events and Proxy

The `events` folder only contains the Event Grid topic's ARM template, and does not require compilation when being manually built.

The `proxy` folder contains the Azure Functions proxy application's ARM template and a `proxies.json` configuration file. The `proxy/proxies` folder should be zipped into a single file.

## Deploying Content Reactor Solution

To deploy Content Reactor into your own Azure subscription, you will need to ensure you follow the correct sequence:

 1. Deploy the Event Grid topic.
 2. Deploy the four microservices (audio, categories, images, text).
 3. Deploy the proxy.
 4. Deploy the web front-end.

The sample includes ARM templates for each component. Each ARM template contains a `uniqueResourceNameSuffix` parameter that must be set to a globally unique value. The instructions for Azure Pipelines below include creating a globablly unique value. When manually deploying the components, if you do not specify your own suffix then the ARM template will create a 13-character random string and use that.

## Deploying Event Grid Topic

The Event Grid topic is deployed using the `events/deploy/template.json` ARM template.

### Deploying Resources Using Azure Pipelines

Create a release configuration with two steps:

 1. **Create Resource Name Suffix:** Use the _PowerShell_ task, with the _Script Path_ set to the relative location of the `CreateUniqueResourceNameSuffix.ps1` file, e.g. `$(System.DefaultWorkingDirectory)/Events-CI/deploy/CreateUniqueResourceNameSuffix.ps1`.

 2. **Deploy ARM Template:** Use the _Azure Resource Group Deployment_ task, with the _Action_ set to `Create or update resource group`. Set the _Template_ to the location of the `template.json` file, e.g. `$(System.DefaultWorkingDirectory)/Events-CI/deploy/template.json`. Set the _Overridable template parameters_ setting to `-uniqueResourceNameSuffix $(UniqueResourceNameSuffix)`.

### Setting up Deployment Manually

The Event Grid topic's ARM template can be manually deployed using the Azure CLI with the following command:

`az group deployment create -g ContentReactor-Events --template-file events/deploy/template.json --mode Complete`

If you want to specify a custom topic name suffix, use the following command instead:

`az group deployment create -g ContentReactor-Events --template-file events/deploy/template.json --mode Complete --parameters uniqueResourceNameSuffix={your-globally-unique-suffix}`

## Deploying Microservices

Each microservice deployment requires multiple steps:

* **Deploy ARM template:** The microservice's ARM template is deployed. Each microservice has a `deploy/microservice.json` file containing its ARM template. The `eventGridTopicName` and `eventsResourceGroupName` parameters should be set to the name of the Event Grid custom topic and the resource group it's contained in. Optionally, the `uniqueResourceNameSuffix` parameter can be set to a unique string that is appended to each resource name, making the names unique across Azure. We recommend doing this explicitly when you are using Azure Pipelines, so that Azure Pipelines can then use the suffix to find the other resource names for the subsequent steps in the deployments.
* **Configure the data storage for each microservice:** For the images and audio microservices, Azure Storage blob containers are created and CORS policies set to allow web clients to access blobs directly. For the categories and text microservices, Cosmos DB containers are created.
* **Deploy application code:** The application code then is deployed into the Azure Functions applications. There are a number of deployment options available for Azure Functions including [Git](https://docs.microsoft.com/en-us/azure/app-service/app-service-deploy-local-git), [cloud folders](https://docs.microsoft.com/en-us/azure/app-service/app-service-deploy-content-sync), [`zip`/`war` files](https://docs.microsoft.com/en-us/azure/app-service/app-service-deploy-zip), and [FTP](https://docs.microsoft.com/en-us/azure/app-service/app-service-deploy-ftp). In the examples below we will use the Azure CLI to perform deployments using `zip` files..
* **Deploy Event Grid subscription ARM templates:** Finally, any necessary Event Grid subscriptions are created on the Event Grid topic, to ensure that relevant event types are forwarded to the microservice. Each microservice that requires Event Grid Subscriptions has an ARM template named `eventGridSubscriptions.json` in its `deploy` folder.

The following sections provide full detail on how to set up the deployments.

Note that the categories microservice requires that you [obtain an API key for Big Huge Thesaurus.](https://words.bighugelabs.com/api.php) If you do not want to obtain a key, you can use a fake key value below; some of the Event Grid functionality for this microservice may not work correctly.

### Deploying Microservices Using Azure Pipelines

When using Azure Pipelines to deploy Content Reactor, a release configuration will need to be created for each microservice. The release configurations will need to be set up with the steps outlined in the sections below. Ensure that you also configure Azure Pipelines with the correct Azure subscription on each step.

### Categories Microservice

 1. **Create Resource Name Suffix:** Use the _PowerShell_ task, with the _Script Path_ set to the relative location of the `CreateUniqueResourceNameSuffix.ps1` file, e.g. `$(System.DefaultWorkingDirectory)/Categories-CI/deploy/CreateUniqueResourceNameSuffix.ps1`.

 2. **Deploy ARM Template:** Use the _Azure Resource Group Deployment_ task, with the _Action_ set to `Create or update resource group`. Set the _Template_ to the location of the `microservice.json` file, e.g. `$(System.DefaultWorkingDirectory)/Categories-CI/deploy/microservice.json`. Set the _Overridable template parameters_ to the following: `-uniqueResourceNameSuffix $(UniqueResourceNameSuffix) -eventsResourceGroupName ContentReactor-Events -eventGridTopicName {event-grid-topic-name} -bigHugeThesaurusApiKey {big-huge-thesaurus-api-key}`

 3. **Create Cosmos DB Database:** Use the _Azure CLI_ task, with the _Inline Script_ set to:

    `az cosmosdb database create --name crcatdb$(UniqueResourceNameSuffix) --db-name Categories --resource-group ContentReactor-Categories & exit 0`

 4. **Create Cosmos DB Collection:** Use the _Azure CLI_ task, with the _Inline Script_ set to:

    `az cosmosdb collection create --name crcatdb$(UniqueResourceNameSuffix) --db-name Categories --collection-name Categories --resource-group ContentReactor-Categories --partition-key-path "/userId" --throughput 1000 & exit 0`

 5. **Deploy API:** Use the _Azure App Service Deploy_ task, with the _App type_ set to `Function app`. Set the _App Service name_ to `crcatapi$(UniqueResourceNameSuffix)`. Set the _Package or folder_ to the relative location of the `.zip` file for the front-end API, e.g. `$(System.DefaultWorkingDirectory)/Categories-CI/functions/ContentReactor.Categories.Api.zip`.

 6. **Deploy Worker API:** Use the _Azure App Service Deploy_ task, with the _App type_ set to `Function app`. Set the _App Service name_ to `crcatwapi$(UniqueResourceNameSuffix)`. Set the _Package or folder_ to the relative location of the `.zip` file for the worker API, e.g. `$(System.DefaultWorkingDirectory)/Categories-CI/functions/ContentReactor.Categories.WorkerApi.zip`.

 7. **Deploy Event Subscriptions ARM Template:** Use the _Azure Resource Group Deployment_ task, with the _Action_ set to `Create or update resource group`. Set the _Template_ to the location of the `eventGridSubscriptions.json` file, e.g. `$(System.DefaultWorkingDirectory)/Categories-CI/deploy/eventGridSubscriptions.json`. Set the _Overridable template parameters_ to the following: `-eventGridTopicName {event-grid-topic-name} -microserviceResourceGroupName ContentReactor-Categories -microserviceFunctionsWorkerApiAppName {worker-api-function-app-name}`

### Text Microservice

 1. **Create Resource Name Suffix:** Use the _PowerShell_ task, with the _Script Path_ set to the relative location of the `CreateUniqueResourceNameSuffix.ps1` file, e.g. `$(System.DefaultWorkingDirectory)/Text-CI/deploy/CreateUniqueResourceNameSuffix.ps1`.

 2. **Deploy ARM Template:** Use the _Azure Resource Group Deployment_ task, with the _Action_ set to `Create or update resource group`. Set the _Template_ to the location of the `microservice.json` file, e.g. `$(System.DefaultWorkingDirectory)/Text-CI/deploy/microservice.json`. Set the _Overridable template parameters_ to the following: `-uniqueResourceNameSuffix $(UniqueResourceNameSuffix) -eventsResourceGroupName ContentReactor-Events -eventGridTopicName {event-grid-topic-name}`

 3. **Create Cosmos DB Database:** Use the _Azure CLI_ task, with the _Inline Script_ set to:

    `az cosmosdb database create --name crtxtdb$(UniqueResourceNameSuffix) --db-name Text --resource-group ContentReactor-Text & exit 0`

 4. **Create Cosmos DB Collection:** Use the _Azure CLI_ task, with the _Inline Script_ set to:

    `az cosmosdb collection create --name crtxtdb$(UniqueResourceNameSuffix) --db-name Text --collection-name Text --resource-group ContentReactor-Text --partition-key-path "/userId" --throughput 1000 & exit 0`

 5. **Deploy API:** Use the _Azure App Service Deploy_ task, with the _App type_ set to `Function app`. Set the _App Service name_ to `crtxtapi$(UniqueResourceNameSuffix)`. Set the _Package or folder_ to the relative location of the `.zip` file for the front-end API, e.g. `$(System.DefaultWorkingDirectory)/Text-CI/functions/ContentReactor.Text.Api.zip`.

Note that for the text microservice there is no worker API, and no Event Grid subscriptions required.

### Images Microservice

 1. **Create Resource Name Suffix:** Use the _PowerShell_ task, with the _Script Path_ set to the relative location of the `CreateUniqueResourceNameSuffix.ps1` file, e.g. `$(System.DefaultWorkingDirectory)/Images-CI/deploy/CreateUniqueResourceNameSuffix.ps1`.

 2. **Deploy ARM Template:** Use the _Azure Resource Group Deployment_ task, with the _Action_ set to `Create or update resource group`. Set the _Template_ to the location of the `microservice.json` file, e.g. `$(System.DefaultWorkingDirectory)/Images-CI/deploy/microservice.json`. Set the _Overridable template parameters_ to the following: `-uniqueResourceNameSuffix $(UniqueResourceNameSuffix) -eventsResourceGroupName ContentReactor-Events -eventGridTopicName {event-grid-topic-name}`

 3. **Create Blob Container - Full Images:** Use the _Azure CLI_ task, with the _Inline Script_ set to:

    `az storage container create --account-name crimgblob$(UniqueResourceNameSuffix) --name fullimages`

 4. **Create Blob Container - Preview Images:** Use the _Azure CLI_ task, with the _Inline Script_ set to:

    `az storage container create --account-name crimgblob$(UniqueResourceNameSuffix) --name previewimages`

 5. **Clear Blob CORS Policy:** Use the _Azure CLI_ task, with the _Inline Script_ set to:

    `az storage cors clear --account-name crimgblob$(UniqueResourceNameSuffix) --services b`

 6. **Set Blob CORS Policy:** Use the _Azure CLI_ task, with the _Inline Script_ set to:

    `az storage cors add --account-name crimgblob$(UniqueResourceNameSuffix) --services b --methods POST GET PUT --origins * --allowed-headers * --exposed-headers *`

 7. **Deploy API:** Use the _Azure App Service Deploy_ task, with the _App type_ set to `Function app`. Set the _App Service name_ to `crimgapi$(UniqueResourceNameSuffix)`. Set the _Package or folder_ to the relative location of the `.zip` file for the front-end API, e.g. `$(System.DefaultWorkingDirectory)/Images-CI/functions/ContentReactor.Images.Api.zip`.

 8. **Deploy Worker API:** Use the _Azure App Service Deploy_ task, with the _App type_ set to `Function app`. Set the _App Service name_ to `crimgwapi$(UniqueResourceNameSuffix)`. Set the _Package or folder_ to the relative location of the `.zip` file for the worker API, e.g. `$(System.DefaultWorkingDirectory)/Images-CI/functions/ContentReactor.Images.WorkerApi.zip`.

 9. **Deploy Event Subscriptions ARM Template:** Use the _Azure Resource Group Deployment_ task, with the _Action_ set to `Create or update resource group`. Set the _Template_ to the location of the `eventGridSubscriptions.json` file, e.g. `$(System.DefaultWorkingDirectory)/Images-CI/deploy/eventGridSubscriptions.json`. Set the _Overridable template parameters_ to the following: `-eventGridTopicName {event-grid-topic-name} -microserviceResourceGroupName ContentReactor-Images -microserviceFunctionsWorkerApiAppName {worker-api-function-app-name}`

### Audio Microservice

 1. **Create Resource Name Suffix:** Use the _PowerShell_ task, with the _Script Path_ set to the relative location of the `CreateUniqueResourceNameSuffix.ps1` file, e.g. `$(System.DefaultWorkingDirectory)/Audio-CI/deploy/CreateUniqueResourceNameSuffix.ps1`.

 2. **Deploy ARM Template:** Use the _Azure Resource Group Deployment_ task, with the _Action_ set to `Create or update resource group`. Set the _Template_ to the location of the `microservice.json` file, e.g. `$(System.DefaultWorkingDirectory)/Audio-CI/deploy/microservice.json`. Set the _Overridable template parameters_ to the following: `-uniqueResourceNameSuffix $(UniqueResourceNameSuffix) -eventsResourceGroupName ContentReactor-Events -eventGridTopicName {event-grid-topic-name}`

 3. **Create Blob Container:** Use the _Azure CLI_ task, with the _Inline Script_ set to:

    `az storage container create --account-name craudblob$(UniqueResourceNameSuffix) --name audio`

 4. **Clear Blob CORS Policy:** Use the _Azure CLI_ task, with the _Inline Script_ set to:

    `az storage cors clear --account-name craudblob$(UniqueResourceNameSuffix) --services b`

 5. **Set Blob CORS Policy:** Use the _Azure CLI_ task, with the _Inline Script_ set to:

    `az storage cors add --account-name craudblob$(UniqueResourceNameSuffix) --services b --methods POST GET PUT --origins * --allowed-headers * --exposed-headers *`

 6. **Deploy API:** Use the _Azure App Service Deploy_ task, with the _App type_ set to `Function app`. Set the _App Service name_ to `craudapi$(UniqueResourceNameSuffix)`. Set the _Package or folder_ to the relative location of the `.zip` file for the front-end API, e.g. `$(System.DefaultWorkingDirectory)/Audio-CI/functions/ContentReactor.Audio.Api.zip`.

 7. **Deploy Worker API:** Use the _Azure App Service Deploy_ task, with the _App type_ set to `Function app`. Set the _App Service name_ to `craudwapi$(UniqueResourceNameSuffix)`. Set the _Package or folder_ to the relative location of the `.zip` file for the worker API, e.g. `$(System.DefaultWorkingDirectory)/Audio-CI/functions/ContentReactor.Audio.WorkerApi.zip`.

 8. **Deploy Event Subscriptions ARM Template:** Use the _Azure Resource Group Deployment_ task, with the _Action_ set to `Create or update resource group`. Set the _Template_ to the location of the `eventGridSubscriptions.json` file, e.g. `$(System.DefaultWorkingDirectory)/Audio-CI/deploy/eventGridSubscriptions.json`. Set the _Overridable template parameters_ to the following: `-eventGridTopicName {event-grid-topic-name} -microserviceResourceGroupName ContentReactor-Audio -microserviceFunctionsWorkerApiAppName {worker-api-function-app-name}`

### Deploying Microservices Manually

To run each microservice deployment manually, you will need to execute a series of steps:

### Deploying Categories Microservice

 1. Deploy the microservice's ARM template:

    `az group deployment create -g ContentReactor-Categories --template-file categories/deploy/microservice.json --parameters eventGridTopicName={event-grid-topic-name} bigHugeThesaurusApiKey={big-huge-thesaurus-api-key} --mode Complete`

 2. Create the Cosmos DB database and container:

    `az cosmosdb database create --name {cosmos-db-account-name} --db-name Categories --resource-group ContentReactor-Categories`

    `az cosmosdb collection create --name {cosmos-db-account-name} --db-name Categories --collection-name Categories --resource-group ContentReactor-Categories --partition-key-path "/userId" --throughput 1000`

 3. Deploy the two Azure Functions apps:

    `az webapp deployment source config-zip --resource-group ContentReactor-Categories --name {api-function-app-name} --src {zip-file-path}`

    `az webapp deployment source config-zip --resource-group ContentReactor-Categories --name {worker-api-function-app-name} --src {zip-file-path}`

 4. Deploy the Event Grid subscription ARM template:

    `az group deployment create -g ContentReactor-Events --template-file categories/deploy/eventGridSubscriptions.json --parameters eventGridTopicName={event-grid-topic-name} microserviceResourceGroupName=ContentReactor-Categories microserviceFunctionsWorkerApiAppName={worker-api-function-app-name}`

### Deploying Text Microservice

 1. Deploy the microservice's ARM template:

    `az group deployment create -g ContentReactor-Text --template-file text/deploy/microservice.json --parameters eventGridTopicName={event-grid-topic-name} --mode Complete`

 2. Create the Cosmos DB database and container:

    `az cosmosdb database create --name {cosmos-db-account-name} --db-name Text --resource-group ContentReactor-Text`

    `az cosmosdb collection create --name {cosmos-db-account-name} --db-name Text --collection-name Text --resource-group ContentReactor-Text --partition-key-path "/userId" --throughput 1000`

 3. Deploy the Azure Functions app:

    `az webapp deployment source config-zip --resource-group ContentReactor-Text --name {api-function-app-name} --src {zip-file-path}`

Note that for the text microservice there is no worker API, and no Event Grid subscriptions required.

### Deploying Images Microservice

 1. Deploy the microservice's ARM template:

    `az group deployment create -g ContentReactor-Images --template-file images/deploy/microservice.json --parameters eventGridTopicName={event-grid-topic-name} --mode Complete`

 2. Create the Azure Storage blob containers:

    `az storage container create --account-name {storage-account-name} --name fullimages`

    `az storage container create --account-name {storage-account-name} --name previewimages`

 3. Add the Azure Storage blob CORS policy:

    `az storage cors clear --account-name {storage-account-name}`

    `az storage cors add --account-name {storage-account-name} --services b --methods POST GET PUT --origins * --allowed-headers * --exposed-headers *`

 4. Deploy the two Azure Functions apps:

    `az webapp deployment source config-zip --resource-group ContentReactor-Images --name {api-function-app-name} --src {zip-file-path}`

    `az webapp deployment source config-zip --resource-group ContentReactor-Images --name {worker-api-function-app-name} --src {zip-file-path}`

 5. Deploy the Event Grid subscription ARM template:

    `az group deployment create -g ContentReactor-Events --template-file images/deploy/eventGridSubscriptions.json --parameters eventGridTopicName={event-grid-topic-name} microserviceResourceGroupName=ContentReactor-Images microserviceFunctionsWorkerApiAppName={worker-api-function-app-name}`

### Deploying Audio Microservice

 1. Deploy the microservice's ARM template:

    `az group deployment create -g ContentReactor-Audio --template-file audio/deploy/microservice.json --parameters eventGridTopicName={event-grid-topic-name} --mode Complete`

 2. Create the Azure Storage blob container:

    `az storage container create --account-name {storage-account-name} --name audio`

 3. Update the Azure Storage blob CORS policy:

    `az storage cors clear --account-name {storage-account-name}`

    `az storage cors add --account-name {storage-account-name} --services b --methods POST GET PUT --origins * --allowed-headers * --exposed-headers *`

 4. Deploy the two Azure Functions apps:

    `az webapp deployment source config-zip --resource-group ContentReactor-Audio --name {api-function-app-name} --src {zip-file-path}`

    `az webapp deployment source config-zip --resource-group ContentReactor-Audio --name {worker-api-function-app-name} --src {zip-file-path}`

 5. Deploy the Event Grid subscription ARM template:

    `az group deployment create -g ContentReactor-Events --template-file audio/deploy/eventGridSubscriptions.json --parameters eventGridTopicName={event-grid-topic-name} microserviceResourceGroupName=ContentReactor-Audio microserviceFunctionsWorkerApiAppName={worker-api-function-app-name}`

## Deploying Proxy

The Azure Functions Proxies app is deployed into its own resource group. It has its own ARM template, which needs to be configured with the names of the front-end APIs for each microservice so that it can handle routing appropriately. It also contains a `proxies.json` file, which is 'compiled' into a `.zip` file in the build steps above.

### Deploying Proxy Using Azure Pipelines

 1. **Create Resource Name Suffix:** Use the _PowerShell_ task, with the _Script Path_ set to the relative location of the `CreateUniqueResourceNameSuffix.ps1` file, e.g. `$(System.DefaultWorkingDirectory)/Proxy-CI/deploy/CreateUniqueResourceNameSuffix.ps1`.

 2. **Deploy ARM Template:** Use the _Azure Resource Group Deployment_ task, with the _Action_ set to `Create or update resource group`. Set the _Template_ to the location of the `template.json` file, e.g. `$(System.DefaultWorkingDirectory)/Proxy-CI/deploy/template.json`. Set the _Overridable template parameters_ to the following: `-uniqueResourceNameSuffix $(UniqueResourceNameSuffix) -categoriesMicroserviceApiAppName {categories-api-function-app-name} -imagesMicroserviceApiAppName {images-api-function-app-name} -audioMicroserviceApiAppName {audio-api-function-app-name} -textMicroserviceApiAppName {text-api-function-app-name}`

 3. **Deploy Proxies Configuration:** Use the _Azure App Service Deploy_ task, with the _App type_ set to `Function app`, and the _App Service name_ set to `crapiproxy$(UniqueResourceNameSuffix)`. Set the _Package or folder_ to the relative location of the `.zip` file for the proxy configuration, e.g. `$(System.DefaultWorkingDirectory)/Proxy-CI/proxies/proxies.zip`.

### Deploying Proxy Manually

 1. Deploy the proxy app's ARM template:

    `az group deployment create -g ContentReactor-Proxy --template-file proxy/deploy/template.json --parameters categoriesMicroserviceApiAppName={categories-api-function-app-name} imagesMicroserviceApiAppName={images-api-function-app-name} audioMicroserviceApiAppName={audio-api-function-app-name} textMicroserviceApiAppName={text-api-function-app-name} --mode Complete`

 2. Deploy the proxies configuration:

    `az webapp deployment source config-zip --resource-group ContentReactor-Proxies --name {proxy-function-app-name} --src {zip-file-path}`

## Deploying Web Application

The web app is deployed into its own resource group. It has its own ARM template which needs to be configured with the Function Proxy App name from the previous step.

### Deploying Web Application Using Azure Pipelines

 1. **Create Resource Name Suffix:** Use the _PowerShell_ task, with the _Script Path_ set to the relative location of the `CreateUniqueResourceNameSuffix.ps1` file, e.g. `$(System.DefaultWorkingDirectory)/Web-CI/deploy/CreateUniqueResourceNameSuffix.ps1`.

 2. **Deploy ARM Template:** Use the _Azure Resource Group Deployment_ task, with the _Action_ set to `Create or update resource group`. Set the _Template_ to the location of the `template.json` file, e.g. `$(System.DefaultWorkingDirectory)/web/deploy/template.json`. Set the _Overridable template parameters_ to the following: `-uniqueResourceNameSuffix $(UniqueResourceNameSuffix) -functionAppProxyName {function-app-proxy-name}`

 3. **Deploy API:** Use the _Azure App Service Deploy_ task, with the _App type_ set to `Web App`. Set the _App Service name_ to `crweb$(UniqueResourceNameSuffix)`. Set the _Package or folder_ to the relative location of the `.zip` file for the front-end API, e.g. `$(System.DefaultWorkingDirectory)/Web-CI/webapp/*.zip`.

 4. **Deploy Event Subscriptions ARM Template:** Use the _Azure Resource Group Deployment_ task, with the _Action_ set to `Create or update resource group`. Set the _Template_ to the location of the `eventGridSubscriptions.json` file, e.g. `$(System.DefaultWorkingDirectory)/web/deploy/eventGridSubscriptions.json`. Set the _Overridable template parameters_ to the following: `eventGridTopicName={event-grid-topic-name}  appServiceName={app-service-name}`

### Deploying Web Application Manually

1. Deploy the web app's ARM template:

     The ARM template creates a web app with a unique string which is generated based on subscription id and resource group id. It accepts the function proxy app name generated in the Proxies Configuration.

    `az group deployment create --name ContentReactorWeb-Deployment --resource-group ContentReactor-Web --template-file web/deploy/template.json --parameters functionAppProxyName={function-proxy-app-name}`

2. Deploy the web app:

     app-service-name is generated when the ARM template is deployed in the previous step.

    `az webapp deployment source config-zip --resource-group ContentReactor-Web --name {app-service-name} --src {zip-file-path}`

3. Deploy the Event Grid subscription ARM Template:

    `az group deployment create -g ContentReactor-Events --template-file web/deploy/eventGridSubscriptions.json --parameters eventGridTopicName={event-grid-topic-name}  appServiceName={app-service-name}`
