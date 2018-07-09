 #!/bin/bash

echo "Checking for prerequisites..."
if ! type npm > /dev/null; then
    echo "Prerequisite Check 1: Install Node.js and NPM"
    exit 1
fi

if ! type dotnet > /dev/null; then
    echo "Prerequisite Check 2: Install .NET Core 2.1 SDK or Runtime"
    exit 1
fi

if ! type zip > /dev/null; then
    echo "Prerequisite Check 3: Install zip"
    exit 1
fi

echo "Prerequisites satisfied"
echo "******* BUILDING ARTIFACTS *******"

shift $((OPTIND - 1))
echo "Building Categories Microservice..."
HOME=`pwd`
cd $HOME/categories/src/ContentReactor.Categories
dotnet build
cd $HOME/categories/src/ContentReactor.Categories/ContentReactor.Categories.Services.Tests
dotnet test
cd $HOME/categories/src/ContentReactor.Categories
dotnet publish -c Release
cd $HOME/categories/src/ContentReactor.Categories/ContentReactor.Categories.Api/bin/Release/netstandard2.0
zip -r ContentReactor.Categories.Api.zip .
cd $HOME/categories/src/ContentReactor.Categories/ContentReactor.Categories.WorkerApi/bin/Release/netstandard2.0
zip -r ContentReactor.Categories.WorkerApi.zip .


echo "Building Images Microservice..."
cd $HOME/images/src/ContentReactor.Images
dotnet build
cd $HOME/images/src/ContentReactor.Images/ContentReactor.Images.Services.Tests
dotnet test
cd $HOME/images/src/ContentReactor.Images
dotnet publish -c Release
cd $HOME/images/src/ContentReactor.Images/ContentReactor.Images.Api/bin/Release/netstandard2.0
zip -r ContentReactor.Images.Api.zip .
cd $HOME/images/src/ContentReactor.Images/ContentReactor.Images.WorkerApi/bin/Release/netstandard2.0
zip -r ContentReactor.Images.WorkerApi.zip .

echo "Building Audio Microservice..."
cd $HOME/audio/src/ContentReactor.Audio
dotnet build
cd $HOME/audio/src/ContentReactor.Audio/ContentReactor.Audio.Services.Tests
dotnet test
cd $HOME/audio/src/ContentReactor.Audio
dotnet publish -c Release
cd $HOME/audio/src/ContentReactor.Audio/ContentReactor.Audio.Api/bin/Release/netstandard2.0
zip -r ContentReactor.Audio.Api.zip .
cd $HOME/audio/src/ContentReactor.Audio/ContentReactor.Audio.WorkerApi/bin/Release/netstandard2.0
zip -r ContentReactor.Audio.WorkerApi.zip .

echo "Building Text Microservice..."
cd $HOME/text/src/ContentReactor.Text
dotnet build
cd $HOME/text/src/ContentReactor.Text/ContentReactor.Text.Services.Tests
dotnet test
cd $HOME/text/src/ContentReactor.Text
dotnet publish -c Release
cd $HOME/text/src/ContentReactor.Text/ContentReactor.Text.Api/bin/Release/netstandard2.0
zip -r ContentReactor.Text.Api.zip .

echo "Building proxy artifact..."
cd $HOME/proxy/proxies
zip -r proxies.zip .

echo "Building Web..."
mkdir $HOME/web/src/signalr-web/SignalRMiddleware/SignalRMiddleware/wwwroot
cd $HOME/web/src/signalr-web/SignalRMiddleware/EventApp
npm install
npm run ubuntu-dev-build
cd $HOME/web/src/signalr-web/SignalRMiddleware/
dotnet build
cd $HOME/web/src/signalr-web/SignalRMiddleware/SignalRMiddlewareTests/
dotnet test
dotnet publish -c Release

cd $HOME/web/src/signalr-web/SignalRMiddleware/SignalRMiddleware/bin/Release/netcoreapp2.1/publish/
zip -r SignalRMiddleware.zip .

echo "Build successfully completed!"
