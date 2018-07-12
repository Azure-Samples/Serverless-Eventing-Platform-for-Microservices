# Serverless Eventing Platform for Microservices

**NOTE:** All deployment templates and steps are designed to find other resources using
a specific naming convention that relies on you providing a **single globally unique
string** __***that only contains letters and numbers***__ 
(it will be part of storage account names) and will prefix each resource name. 
This includes the resource groups.
Pick a value now (e.g. 'cr2018') and substitue it where ever you see 
`{your-globally-unique-prefix}`

## Deploying Content Reactor

Content Reactor can be built and deployed into your own Azure subscription. 
You can use VSTS to run builds and releases, or if you prefer, 
you can run the build and deployment steps manually. 
We assume that you have an Azure subscription available to run the sample; 
[you can get a free trial Azure subscription here](https://azure.microsoft.com/en-us/free/).

This guide explains the steps to build and deploy Content Reactor, 
both using VSTS and manually. For manual deployments we suggest using the 
[Azure CLI](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli?view=azure-cli-latest), 
a cross-platform command-line interface for Azure.

The overall sequence involved in building and deploying Content Reactor is:

 1. **Creating resource groups.**
 2. **Building Content Reactor.**
 3. **Deploying Content Reactor.** Each component and microservice has its 
 own deployment process, which is described in detail below.

## Creating Resource Groups

Each component of Content Reactor - each microservice, the instance of app insights, 
the Event Grid topic, the web front-end, and the API proxy - 
should be deployed into their own resource group.

We have used the following resource group names:

* `{your-globally-unique-prefix}-audio`
* `{your-globally-unique-prefix}-categories`
* `{your-globally-unique-prefix}-events`
* `{your-globally-unique-prefix}-images`
* `{your-globally-unique-prefix}-proxy`
* `{your-globally-unique-prefix}-text`
* `{your-globally-unique-prefix}-web`

As this is a one-time activity, you may decide to use the 
[Azure Portal to create the resource groups](https://docs.microsoft.com/en-us/azure/azure-resource-manager/resource-group-portal#manage-resource-groups). 
We recommend placing the resources in the `West US 2` region, 
since it allows for all of the resource types we use. 
Other regions that support Event Grid (
[see a list of these regions here](https://docs.microsoft.com/en-us/azure/event-grid/overview)) 
may also be used.

Alternatively, you may also prefer to use the Azure CLI to create the resource groups. 
After [logging into your Azure subscription](https://docs.microsoft.com/en-us/cli/azure/authenticate-azure-cli?view=azure-cli-latest) 
you can execute the following commands to create the resource groups:

* `az group create -n {your-globally-unique-prefix}-audio -l westus2`
* `az group create -n {your-globally-unique-prefix}-categories -l westus2`
* `az group create -n {your-globally-unique-prefix}-events -l westus2`
* `az group create -n {your-globally-unique-prefix}-images -l westus2`
* `az group create -n {your-globally-unique-prefix}-proxy -l westus2`
* `az group create -n {your-globally-unique-prefix}-text -l westus2`
* `az group create -n {your-globally-unique-prefix}-web -l westus2`

## Building Content Reactor

### Build Using VSTS

Each of the subfolders in this repository (`audio`, `categories`, `events`, 
`images`, `proxy`, `text`, and `web`) 
contains a `build` subfolder with a `build.yaml` file. The `build.yaml` 
files contain the list of VSTS build steps that are required for that component.

To use VSTS to build the Content Reactor system, you will need to set up multiple 
build configurations - one for each component with a `build.yaml` file. 
[Follow the instructions here](https://docs.microsoft.com/en-us/vsts/build-release/actions/build-yaml?view=vsts#manually-create-a-yaml-build-definition) 
to create each build definition and select the appropriate `build.yaml` file.

After all the build definitions have been created, queue builds using those definitions

### Build Manually

Each microservice's folder (`audio`, `categories`, `images`, and `text`) 
contains a Visual Studio solution within the `src` subfolder. 
The solutions can be built using Visual Studio 2017 or higher, or by using the 
`dotnet build` CLI command. The application artifacts can be collected using the 
`dotnet publish` command. Unit tests can be executed by using the `dotnet test` 
command. Once a microservice is built, the published API projects should be collected 
into `.zip` files to prepare for deployment.

For example, to build the categories microservice, you would execute the following 
commands from within the `categories/src/ContentReactor.Categories` subfolder:

 1. `dotnet build`
 2. `dotnet test`
 3. `dotnet publish -c Release`
 4. Zip the contents of the `ContentReactor.Categories.Api/bin/Release/netstandard2.0` 
folder and name it `ContentReactor.Categories.Api.zip`.
 5. Zip the contents of the `ContentReactor.Categories.WorkerApi/bin/Release/netstandard2.0` 
folder and name it `ContentReactor.Categories.WorkerApi.zip`.

#### Building Web Manually

The front end and the signalR middleware is in the signalr-web/SignalRMiddleware folder. 
This folder has a solution file which can be built using Visual Studio Version 15.7.0 
and above. The build takes care of building the angular component and bundling this 
with the ASP.NET WebApp.

Note: Please make sure that your build host or your local machine has the latest 
versions of node.js and npm installed.

Note: The ASP.NET app works on a preview version of .NET Core.
The instructions to install the preview locally can be found 
[here](https://blogs.msdn.microsoft.com/webdev/2018/02/27/asp-net-core-2-1-0-preview1-using-asp-net-core-previews-on-azure-app-service/)

Build the ASP.NET Web Application using the following steps:

1. Build the Angular app:
    Go to web/src/signalr-web/SignalRMiddleware/EventApp 
	and perform `npm install` followed by `npm run dev`

2. Restore NuGet packages:
    `dotnet restore web/src/signalr-web/SignalRMiddleware/SignalRMiddleware.sln`

3. Build MVC App: This will bundle Angular app and ASP.NET app using MSBuild
    `dotnet build  web\src\signalr-web\SignalRMiddleware\SignalRMiddleware.sln`

4. Zip the contents of web\src\signalr-web\SignalRMiddleware\SignalRMiddleware\obj\Release\netcoreapp2.1

#### Building Events, Monitor and Proxy Manually

The `events` and 'monitor' folders only contain the Event Grid topic's ARM template 
and the App Insights template, and does not require compilation when being manually built.

The `proxy` folder contains the Azure Functions proxy application's ARM template 
and a `proxies.json` configuration file. The `proxy/proxies` folder should be 
zipped into a single file.

## Deploying Content Reactor Solution

To deploy Content Reactor into your own Azure subscription, you will need to ensure 
you follow the correct sequence:

 2. Deploy the Event Grid topic.
 3. Deploy the four microservices (audio, categories, images, text).
 4. Deploy the proxy.
 5. Deploy the web front-end.

The sample includes ARM templates for each component. Each ARM template contains a 
`uniqueResourceNamePrefix` parameter that must be set to a globally unique value. 
The instructions for VSTS below include creating a globablly unique value. 

### Deploying Event Grid Topic

The Event Grid topic is deployed using the `events/deploy/template.json` ARM template.

#### Deploying Event Grid Topic Using VSTS

**Deploy ARM Template:** Use the _Azure Resource Group Deployment_ task, 
with the _Action_ set to `Create or update resource group`. 
Set the _Template_ to the location of the `template.json` file, 
.g. `$(System.DefaultWorkingDirectory)/Events-CI/deploy/template.json`. 
Set the _Overridable template parameters_ setting to 
`-uniqueResourceNamePrefix {your-globally-unique-prefix}`.

#### Setting up Deployment Manually

The Event Grid topic's ARM template can be manually deployed using the Azure CLI 
with the following command:

`az group deployment create -g {your-globally-unique-prefix}-events 
--template-file events/deploy/template.json --mode Complete 
--parameters uniqueResourceNamePrefix={your-globally-unique-prefix}`

## Deploying Microservices

Each microservice deployment requires multiple steps:

* **Deploy ARM template:** The microservice's ARM template is deployed. 
Each microservice has a `deploy/microservice.json` file containing its ARM template. 
The `uniqueResourceNamePrefix` parameter should be set to `{your-globally-unique-prefix}` 
string that is prepended to each resource name, making the names unique across Azure. 
* **Configure the data storage for each microservice:** 
For the images and audio microservices, Azure Storage blob containers 
are created and CORS policies set to allow web clients to access blobs directly. 
For the categories and text microservices, Cosmos DB containers are created.
* **Deploy application code:** The application code then is deployed into the 
Azure Functions applications. There are a number of deployment options available 
for Azure Functions including 
[Git](https://docs.microsoft.com/en-us/azure/app-service/app-service-deploy-local-git), 
[cloud folders](https://docs.microsoft.com/en-us/azure/app-service/app-service-deploy-content-sync), 
[`zip`/`war` files](https://docs.microsoft.com/en-us/azure/app-service/app-service-deploy-zip), 
and [FTP](https://docs.microsoft.com/en-us/azure/app-service/app-service-deploy-ftp). 
In the examples below we will use the Azure CLI to perform deployments using `zip` files.
* **Deploy Event Grid subscription ARM templates:** Finally, 
any necessary Event Grid subscriptions are created on the Event Grid topic, 
to ensure that relevant event types are forwarded to the microservice. 
Each microservice that requires Event Grid Subscriptions has an ARM template named 
`eventGridSubscriptions.json` in its `deploy` folder.

The following sections provide full detail on how to set up the deployments.

Note that the categories microservice requires that you 
[obtain an API key for Big Huge Thesaurus.](https://words.bighugelabs.com/api.php) 
If you do not want to obtain a key, you can use a fake key value below; 
some of the Event Grid functionality for this microservice may not work correctly.

### Deploying Microservices Using VSTS

When using VSTS to deploy Content Reactor, a release configuration will need to be created for each microservice. The release configurations will need to be set up with the steps outlined in the sections below. Ensure that you also configure VSTS with the correct Azure subscription on each step.

#### Deploying Categories Microservice using VSTS

 1. **Deploy ARM Template:** Use the _Azure Resource Group Deployment_ task, 
    with the _Action_ set to `Create or update resource group`. 
    Set the _Template_ to the location of the `microservice.json` file, 
    e.g. `$(System.DefaultWorkingDirectory)/Categories-CI/deploy/microservice.json`. 
    Set the _Overridable template parameters_ to the following: 
    `-uniqueResourceNamePrefix {your-globally-unique-prefix} -bigHugeThesaurusApiKey {big-huge-thesaurus-api-key}`

 3. **Create Cosmos DB Database:** Use the _Azure CLI_ task, with the _Inline Script_ set to:

    `az cosmosdb database create --name {your-globally-unique-prefix}-categories-db 
--db-name Categories --resource-group {your-globally-unique-prefix}-categories 
& exit 0`

 4. **Create Cosmos DB Collection:** Use the _Azure CLI_ task, with the _Inline Script_ set to:

    `az cosmosdb collection create --name {your-globally-unique-prefix}-categories-db 
--db-name Categories --collection-name Categories 
--resource-group {your-globally-unique-prefix}-categories 
--partition-key-path "/userId" --throughput 400 & exit 0`

 5. **Deploy API:** Use the _Azure App Service Deploy_ task, with the _App type_ 
set to `Function app`. Set the _App Service name_ to `{your-globally-unique-prefix}-categories-api`. 
Set the _Package or folder_ to the relative location of the `.zip` file for 
the front-end API, e.g. 
`$(System.DefaultWorkingDirectory)/Categories-CI/functions/ContentReactor.Categories.Api.zip`.

 6. **Deploy Worker API:** Use the _Azure App Service Deploy_ task, 
with the _App type_ set to `Function app`. Set the _App Service name_ 
to `{your-globally-unique-prefix}-categories-worker`. 
Set the _Package or folder_ to the relative location of the `.zip` file 
for the worker API, e.g. `$(System.DefaultWorkingDirectory)/Categories-CI/functions/ContentReactor.Categories.WorkerApi.zip`.

 7. **Deploy Event Subscriptions ARM Template:** Use the _Azure Resource Group Deployment_ 
task, with the _Action_ set to `Create or update resource group`. 
Set the _Template_ to the location of the `eventGridSubscriptions.json` file, e.g. 
`$(System.DefaultWorkingDirectory)/Categories-CI/deploy/eventGridSubscriptions.json`. 
Set the _Overridable template parameters_ to the following: 
`-uniqueResourceNamePrefix {your-globally-unique-prefix}`

#### Deploying Text Microservice using VSTS

 1. **Deploy ARM Template:** Use the _Azure Resource Group Deployment_ task, 
with the _Action_ set to `Create or update resource group`. Set the _Template_ 
to the location of the `microservice.json` file, e.g. 
`$(System.DefaultWorkingDirectory)/Text-CI/deploy/microservice.json`. 
Set the _Overridable template parameters_ to the following: 
`-uniqueResourceNamePrefix {your-globally-unique-prefix}`

 3. **Create Cosmos DB Database:** Use the _Azure CLI_ task, with the _Inline Script_ set to:

    `az cosmosdb database create --name {your-globally-unique-prefix}-text-db 
--db-name Text --resource-group {your-globally-unique-prefix}-text & exit 0`

 4. **Create Cosmos DB Collection:** Use the _Azure CLI_ task, with the _Inline Script_ set to:

    `az cosmosdb collection create --name {your-globally-unique-prefix}-text-db 
--db-name Text --collection-name Text 
--resource-group {your-globally-unique-prefix}-text 
--partition-key-path "/userId" --throughput 400 & exit 0`

 5. **Deploy API:** Use the _Azure App Service Deploy_ task, with the _App type_ set to 
`Function app`. Set the _App Service name_ to `{your-globally-unique-prefix}-text-api`. 
Set the _Package or folder_ to the relative location of the `.zip` file 
for the front-end API, e.g. 
`$(System.DefaultWorkingDirectory)/Text-CI/functions/ContentReactor.Text.Api.zip`.

Note that for the text microservice there is no worker API, 
and no Event Grid subscriptions required.

#### Deploying Images Microservice using VSTS

 1. **Deploy ARM Template:** Use the _Azure Resource Group Deployment_ task, 
with the _Action_ set to `Create or update resource group`. 
Set the _Template_ to the location of the `microservice.json` file, e.g. 
`$(System.DefaultWorkingDirectory)/Images-CI/deploy/microservice.json`. 
Set the _Overridable template parameters_ to the following: 
`-uniqueResourceNamePrefix {your-globally-unique-prefix}`

 3. **Create Blob Container - Full Images:** Use the _Azure CLI_ task, with the 
_Inline Script_ set to:

    `az storage container create --account-name {your-globally-unique-prefix}imagesblob 
--name fullimages`

 4. **Create Blob Container - Preview Images:** Use the _Azure CLI_ task, with the 
_Inline Script_ set to:

    `az storage container create --account-name {your-globally-unique-prefix}imagesblob 
--name previewimages`

 5. **Clear Blob CORS Policy:** Use the _Azure CLI_ task, with the _Inline Script_ set to:

    `az storage cors clear --account-name {your-globally-unique-prefix}imagesblob 
--services b`

 6. **Set Blob CORS Policy:** Use the _Azure CLI_ task, with the _Inline Script_ set to:

    `az storage cors add --account-name {your-globally-unique-prefix}imagesblob 
--services b --methods POST GET PUT --origins * --allowed-headers * 
--exposed-headers *`

 7. **Deploy API:** Use the _Azure App Service Deploy_ task, with the _App type_ set to 
`Function app`. Set the _App Service name_ to `{your-globally-unique-prefix}-images-api`. 
Set the _Package or folder_ to the relative location of the `.zip` file for the front-end API, 
e.g. `$(System.DefaultWorkingDirectory)/Images-CI/functions/ContentReactor.Images.Api.zip`.

 8. **Deploy Worker API:** Use the _Azure App Service Deploy_ task, with the _App type_ 
set to `Function app`. Set the _App Service name_ to `{your-globally-unique-prefix}-images-worker`. 
Set the _Package or folder_ to the relative location of the `.zip` file for the worker API, 
e.g. `$(System.DefaultWorkingDirectory)/Images-CI/functions/ContentReactor.Images.WorkerApi.zip`.

 9. **Deploy Event Subscriptions ARM Template:** Use the _Azure Resource Group Deployment_ 
task, with the _Action_ set to `Create or update resource group`. 
Set the _Template_ to the location of the `eventGridSubscriptions.json` file, 
e.g. `$(System.DefaultWorkingDirectory)/Images-CI/deploy/eventGridSubscriptions.json`. 
Set the _Overridable template parameters_ to the following: 
`-uniqueResourceNamePrefix {your-globally-unique-prefix}`

#### Deploying Audio Microservice using VSTS

 2. **Deploy ARM Template:** Use the _Azure Resource Group Deployment_ task, 
with the _Action_ set to `Create or update resource group`. Set the _Template_ 
to the location of the `microservice.json` file, e.g. 
`$(System.DefaultWorkingDirectory)/Audio-CI/deploy/microservice.json`. 
Set the _Overridable template parameters_ to the following: 
`-uniqueResourceNamePrefix {your-globally-unique-prefix}`

 3. **Create Blob Container:** Use the _Azure CLI_ task, with the _Inline Script_ set to:

    `az storage container create --account-name {your-globally-unique-prefix}audioblob 
--name audio`

 4. **Clear Blob CORS Policy:** Use the _Azure CLI_ task, with the _Inline Script_ set to:

    `az storage cors clear --account-name {your-globally-unique-prefix}audioblob 
--services b`

 5. **Set Blob CORS Policy:** Use the _Azure CLI_ task, with the _Inline Script_ set to:

    `az storage cors add --account-name {your-globally-unique-prefix}audioblob 
--services b --methods POST GET PUT --origins * --allowed-headers * 
--exposed-headers *`

 6. **Deploy API:** Use the _Azure App Service Deploy_ task, with the _App type_ set to 
`Function app`. Set the _App Service name_ to `{your-globally-unique-prefix}-audio-api`. 
Set the _Package or folder_ to the relative location of the `.zip` file for the front-end API, 
e.g. `$(System.DefaultWorkingDirectory)/Audio-CI/functions/ContentReactor.Audio.Api.zip`.

 7. **Deploy Worker API:** Use the _Azure App Service Deploy_ task, with the _App type_ 
set to `Function app`. Set the _App Service name_ to 
`{your-globally-unique-prefix}-audio-worker`. 
Set the _Package or folder_ to the relative location of the `.zip` file for the worker API, 
e.g. `$(System.DefaultWorkingDirectory)/Audio-CI/functions/ContentReactor.Audio.WorkerApi.zip`.

 8. **Deploy Event Subscriptions ARM Template:** Use the _Azure Resource Group Deployment_ 
task, with the _Action_ set to `Create or update resource group`. 
Set the _Template_ to the location of the `eventGridSubscriptions.json` file, e.g. 
`$(System.DefaultWorkingDirectory)/Audio-CI/deploy/eventGridSubscriptions.json`. 
Set the _Overridable template parameters_ to the following: 
`-uniqueResourceNamePrefix {your-globally-unique-prefix}`

### Deploying Microservices Manually

To run each microservice deployment manually, you will need to execute a series of steps:

#### Deploying Categories Microservice Manually

 1. Deploy the microservice's ARM template:

    `az group deployment create -g {your-globally-unique-prefix}-categories 
template-file categories/deploy/microservice.json 
--parameters uniqueResourceNamePrefix={your-globally-unique-prefix} 
bigHugeThesaurusApiKey={big-huge-thesaurus-api-key} --mode Complete`

 2. Create the Cosmos DB database and container:

    `az cosmosdb database create --name {your-globally-unique-prefix}-categories-db 
--db-name Categories --resource-group {your-globally-unique-prefix}-categories`

    `az cosmosdb collection create --name {your-globally-unique-prefix}-categories-db 
--db-name Categories --collection-name Categories 
--resource-group {your-globally-unique-prefix}-categories 
--partition-key-path "/userId" --throughput 400`

 3. Deploy the two Azure Functions apps:

    `az webapp deployment source config-zip 
--resource-group {your-globally-unique-prefix}-categories 
--name {your-globally-unique-prefix}-categories-api --src {zip-file-path}`

    `az webapp deployment source config-zip 
--resource-group {your-globally-unique-prefix}-categories 
--name {your-globally-unique-prefix}-categories-worker --src {zip-file-path}`

 4. Deploy the Event Grid subscription ARM template:

    `az group deployment create -g {your-globally-unique-prefix}-events 
--template-file categories/deploy/eventGridSubscriptions.json 
--parameters uniqueResourceNamePrefix={your-globally-unique-prefix}`

#### Deploying Text Microservice Manually

 1. Deploy the microservice's ARM template:

    `az group deployment create -g {your-globally-unique-prefix}-text 
--template-file text/deploy/microservice.json 
--parameters uniqueResourceNamePrefix={your-globally-unique-prefix} --mode Complete`

 2. Create the Cosmos DB database and container:

    `az cosmosdb database create --name {your-globally-unique-prefix}-text-db 
--db-name Text --resource-group {your-globally-unique-prefix}-text`

    `az cosmosdb collection create --name {your-globally-unique-prefix}-text-db 
--db-name Text --collection-name Text --resource-group {your-globally-unique-prefix}-text 
--partition-key-path "/userId" --throughput 400`

 3. Deploy the Azure Functions app:

    `az webapp deployment source config-zip --resource-group {your-globally-unique-prefix}-text 
--name {your-globally-unique-prefix}-text-api --src {zip-file-path}`

Note that for the text microservice there is no worker API, and no Event Grid subscriptions required.

#### Deploying Images Microservice Manually

 1. Deploy the microservice's ARM template:

    `az group deployment create -g {your-globally-unique-prefix}-images 
--template-file images/deploy/microservice.json 
--parameters uniqueResourceNamePrefix={your-globally-unique-prefix} --mode Complete`

 2. Create the Azure Storage blob containers:

    `az storage container create --account-name {your-globally-unique-prefix}imagesblob 
--name fullimages`

    `az storage container create --account-name {your-globally-unique-prefix}imagesblob 
--name previewimages`

 3. Add the Azure Storage blob CORS policy:

    `az storage cors clear --account-name {your-globally-unique-prefix}imagesblob`

    `az storage cors add --account-name {your-globally-unique-prefix}imagesblob 
--services b --methods POST GET PUT --origins * --allowed-headers * --exposed-headers *`

 4. Deploy the two Azure Functions apps:

    `az webapp deployment source config-zip --resource-group {your-globally-unique-prefix}-images 
--name {your-globally-unique-prefix}-images-api --src {zip-file-path}`

    `az webapp deployment source config-zip --resource-group {your-globally-unique-prefix}-Images 
--name {your-globally-unique-prefix}-images-worker --src {zip-file-path}`

 5. Deploy the Event Grid subscription ARM template:

    `az group deployment create -g {your-globally-unique-prefix}-Events 
--template-file images/deploy/eventGridSubscriptions.json 
--parameters uniqueResourceNamePrefix={your-globally-unique-prefix}`

#### Deploying Audio Microservice Manually

 1. Deploy the microservice's ARM template:

    `az group deployment create -g {your-globally-unique-prefix}-Audio 
--template-file audio/deploy/microservice.json 
--parameters uniqueResourceNamePrefix={your-globally-unique-prefix} --mode Complete`

 2. Create the Azure Storage blob container:

    `az storage container create --account-name {your-globally-unique-prefix}audioblob 
--name audio`

 3. Update the Azure Storage blob CORS policy:

    `az storage cors clear --account-name {your-globally-unique-prefix}audioblob`

    `az storage cors add --account-name {your-globally-unique-prefix}audioblob 
--services b --methods POST GET PUT --origins * --allowed-headers * 
--exposed-headers *`

 4. Deploy the two Azure Functions apps:

    `az webapp deployment source config-zip --resource-group {your-globally-unique-prefix}-audio 
--name {your-globally-unique-prefix}-audio-api --src {zip-file-path}`

    `az webapp deployment source config-zip --resource-group {your-globally-unique-prefix} 
--name {your-globally-unique-prefix}-audio-worker --src {zip-file-path}`

 5. Deploy the Event Grid subscription ARM template:

    `az group deployment create -g {your-globally-unique-prefix}-events 
--template-file audio/deploy/eventGridSubscriptions.json 
--parameters uniqueResourcePrefixName={your-globally-unique-prefix}`

### Deploying Proxy

The Azure Functions Proxies app is deployed into its own resource group. 
It has its own ARM template, which needs to be configured with the names of the 
front-end APIs for each microservice so that it can handle routing appropriately. 
It also contains a `proxies.json` file, which is 'compiled' into a `.zip` file in 
the build steps above.

#### Deploying Proxy Using VSTS

 1. **Deploy ARM Template:** Use the _Azure Resource Group Deployment_ task, with 
the _Action_ set to `Create or update resource group`. Set the _Template_ to 
the location of the `template.json` file, 
e.g. `$(System.DefaultWorkingDirectory)/Proxy-CI/deploy/template.json`. 
Set the _Overridable template parameters_ to the following: 
`-uniqueResourceNamePrefix {your-globally-unique-prefix}`

 3. **Deploy Proxies Configuration:** Use the _Azure App Service Deploy_ task, 
with the _App type_ set to `Function app`, and the _App Service name_ set to 
`{your-globally-unique-prefix}-proxy-api`. Set the _Package or folder_ to 
the relative location of the `.zip` file for the proxy configuration, e.g. 
`$(System.DefaultWorkingDirectory)/Proxy-CI/proxies/proxies.zip`.

#### Deploying Proxy Manually

 1. Deploy the proxy app's ARM template:

    `az group deployment create -g {your-globally-unique-prefix}-proxy 
--template-file proxy/deploy/template.json 
--parameters uniqueResourceNamePrefix={your-globally-unique-prefix} 
--mode Complete`

 2. Deploy the proxies configuration:

    `az webapp deployment source config-zip --resource-group {your-globally-unique-prefix}-proxy 
--name {your-globally-unique-prefix}-proxy-api --src {zip-file-path}`

### Deploying Web Application

The web app is deployed into its own resource group. It has its own ARM template 
which needs to be configured with the Function Proxy App name from the previous step.

#### Deploying Web Application Using VSTS

 1. **Deploy ARM Template:** Use the _Azure Resource Group Deployment_ task, 
with the _Action_ set to `Create or update resource group`. 
Set the _Template_ to the location of the `template.json` file, 
e.g. `$(System.DefaultWorkingDirectory)/web/deploy/template.json`. 
Set the _Overridable template parameters_ to the following: 
`-uniqueResourceNamePrefix {your-globally-unique-prefix}`

 3. **Deploy API:** Use the _Azure App Service Deploy_ task, with the _App type_ 
set to `Web App`. Set the _App Service name_ to `{your-globally-unique-prefix}-web-app`. 
Set the _Package or folder_ to the relative location of the `.zip` 
file for the front-end API, e.g. 
`$(System.DefaultWorkingDirectory)/Web-CI/webapp/*.zip`.

 4. **Deploy Event Subscriptions ARM Template:** Use the _Azure Resource Group Deployment_ 
task, with the _Action_ set to `Create or update resource group`. 
Set the _Template_ to the location of the `eventGridSubscriptions.json` file, 
e.g. `$(System.DefaultWorkingDirectory)/web/deploy/eventGridSubscriptions.json`. 
Set the _Overridable template parameters_ to the following: 
`uniqueResourceNamePrefix={event-grid-topic-name}`


#### Deploying Web Application Manually

1. Deploy the web app's ARM template:

     The ARM template creates a web app with a unique string which is generated 
based on subscription id and resource group id. It accepts the function proxy app name 
generated in the Proxies Configuration.

    `az group deployment create --name {your-globally-unique-prefix}-web-deployment 
--resource-group {your-globally-unique-prefix}-web 
--template-file web/deploy/template.json 
--parameters uniqueResourceNamePrefix={your-globally-unique-prefix}`

2. Deploy the web app:

     app-service-name is generated when the ARM template is deployed in the previous step.

    `az webapp deployment source config-zip 
--resource-group {your-globally-unique-prefix}-web 
--name {your-globally-unique-prefix}-web-app 
--src {zip-file-path}`

3. Deploy the Event Grid subscription ARM Template:

    `az group deployment create -g {your-globally-unique-prefix}-events 
--template-file web/deploy/eventGridSubscriptions.json 
--parameters uniqueResourceNamePrefix={your-globally-unique-prefix}`
