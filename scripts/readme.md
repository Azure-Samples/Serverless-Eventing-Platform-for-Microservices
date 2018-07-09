# Content Reactor: Build and Deployment Scripts

The build.sh and deploy.sh scripts are meant to be run in Ubuntu WSL. Here are the pre-requisite installations before these scripts can be run:

1. Install Ubuntu WSL or you can use a Ubuntu bash shell
2. Run fromdos command on both these scripts to convert them from dos to unix.
    
    sudo apt-get update
    sudo apt-get install tofrodos

3. The command 'which node' should point to a node installation in ubuntu (eg: /usr/bin/node)

    curl -sL https://deb.nodesource.com/setup_8.x | sudo -E bash –
    sudo apt-get install -y nodejs
    
4. The command 'which npm' should point to an npm installation in ubuntu (eg: /usr/bin/npm)
5. Make sure 'node --version' returns a Node.js version > 8 (eg: v8.11.3)
6. Make sure dotnet cli is installed and points to version 2.1.x. See install instructions [here](https://www.microsoft.com/net/learn/get-started/linux/ubuntu16-04)
7. Install latest [Azure CLI](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli-apt?view=azure-cli-latest)
8. [Create an Azure Service Principal](https://docs.microsoft.com/en-us/cli/azure/create-an-azure-service-principal-azure-cli?view=azure-cli-latest) with password for your subscription id and note down the following
    1. Service Principal App Id
    2. Service Principal App Password
    3. Tenant ID
    4. Your azure subscription id (you can do "az account list" command to get the id of the subscription you need)
9. Big Huge Thesaurus is an external API used by one of the microservices in this sample. Make sure you get a thesaurus key [here](https://words.bighugelabs.com/api.php) 

From the root folder of the repository execute the following commands on the Ubuntu WSL:
1. sh scripts/build.sh
2. sh scripts/deploy.sh


