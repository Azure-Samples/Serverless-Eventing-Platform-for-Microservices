#!/bin/bash

HOME=`pwd`

C="\033[1;35m"
D=`date +%Y-%m-%d-%H:%M:%S`
NC='\033[0m'

# Deploy Web
echo -e "$C$D Creating web resource group for $1.$NC"
time az group create -n $1"-web" -l westus2
echo -e "$C$D Created web resource group for $1.$NC"

echo -e "$C$D Executing web deployment for $1.$NC"
time az group deployment create -g $1"-web" --template-file $HOME/web/deploy/template.json --parameters uniqueResourceNamePrefix=$1
echo -e "$C$D Executed web deployment for $1.$NC"

echo -e "$C$D Updating web environment.js %INSTRUMENTATION_KEY%"
WEB_APP_NAME=$1"-web-app"
time webInstrumentationKey=$(az resource show --namespace microsoft.insights --resource-type components --name $1-web-ai -g $1-web --query properties.InstrumentationKey)
time sed -i -e 's/\"%INSTRUMENTATION_KEY%\"/'"$webInstrumentationKey"'/g' $HOME/web/src/signalr-web/SignalRMiddleware/EventApp/src/environments/environment.ts
time sed -i -e 's/\"%INSTRUMENTATION_KEY%\"/'"$webInstrumentationKey"'/g' $HOME/web/src/signalr-web/SignalRMiddleware/EventApp/src/environments/environment.prod.ts
echo -e "$C$D Updated web environment.js %INSTRUMENTATION_KEY%"

echo -e "$C$D Running npm install for $1.$NC"
cd $HOME/web/src/signalr-web/SignalRMiddleware/EventApp
time npm install
echo -e "$C$D Ran npm install for $1.$NC"

echo -e "$C$D Running npm unbuntu-dev-build for $1.$NC"
time npm run ubuntu-dev-build
echo -e "$C$D Ran npm unbuntu-dev-build for $1.$NC"

echo -e "$C$D Running dotnet publish for $1.$NC"
cd $HOME/web/src/signalr-web/SignalRMiddleware/SignalRMiddleware
time dotnet publish -c Release
echo -e "$C$D Ran dotnet publish for $1.$NC"

echo -e "$C$D Running zip for $1.$NC"
cd $HOME/web/src/signalr-web/SignalRMiddleware/SignalRMiddleware/bin/Release/netcoreapp2.1/publish/
time zip -r SignalRMiddleware.zip .
echo -e "$C$D Ran zip for $1.$NC"

echo -e "$C$D Deploying the web app for $1.$NC"
time az webapp deployment source config-zip --resource-group $1"-web" --name $WEB_APP_NAME --src $HOME/web/src/signalr-web/SignalRMiddleware/SignalRMiddleware/bin/Release/netcoreapp2.1/publish/SignalRMiddleware.zip
echo -e "$C$D Deployed the web app for $1.$NC"

echo -e "$C$D Creating the web app event grid subscription for $1.$NC"
time az group deployment create -g $1"-events" --template-file $HOME/web/deploy/eventGridSubscriptions-web.json --parameters uniqueResourceNamePrefix=$1
echo -e "$C$D Created the web app event grid subscription for $1.$NC"

echo -e "$C$D Completed web deployment for $1.$NC for $1.$NC"
