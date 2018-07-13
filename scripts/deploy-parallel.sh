#!/bin/bash

HOME=`pwd`

C="\033[1;35m"
D=`date +%Y-%m-%d-%H:%M:%S`
NC='\033[0m'

if [ "$#" -eq 6 ]; then
	servicePrincipalAppId="$1"
	echo "$C$D Service Principal App Id: $servicePrincipalAppId$NC"
	servicePrincipalPassword="$2"
	echo "$C$D Service Principal Password: ********$NC"
	servicePrincipalTenantId="$3"
	echo "$C$D Service Principal Tenant Id: $servicePrincipalTenantId$NC"
	subscriptionId="$4"
	echo "$C$D Subscription Id: $subscriptionId$NC"
	uniquePrefixString="$5"
	echo "$C$D Unique Prefix String: $uniquePrefixString$NC"
	bigHugeThesaurusApiKey="$6"
else
	echo "$C$D Provide Service Principal App ID: $NC"
	read servicePrincipalAppId
	echo "$C$D Provide Service Principal Password: $NC"
	read servicePrincipalPassword
	echo "$C$D Provide Service Principal Tenant ID: $NC"
	read servicePrincipalTenantId
	echo "$C$D Provide subscription ID: $NC"
	read subscriptionId
	echo "$C$D Provide any unique Prefix string (max length 15 characters, recommended to autogenerate a string): $NC"
	read uniquePrefixString
	echo "$C$D Provide Big Huge Thesaurus API Key: $NC"
	read bigHugeThesaurusApiKey
fi


echo "$C$D Logging in.$NC"
time az login --service-principal --username $servicePrincipalAppId --password $servicePrincipalPassword --tenant $servicePrincipalTenantId
echo "$C$D Setting subscription.$NC"
time az account set --subscription $subscriptionId 

# Creating Event Grid Topic
time ./scripts/deploy-events.sh $uniquePrefixString

# Categories Microservice Deploy
time ./scripts/deploy-categories.sh $uniquePrefixString $bigHugeThesaurusApiKey &

# Images Microservice Deploy

time ./scripts/deploy-images.sh $uniquePrefixString &

# Audio Microservice Deploy

time ./scripts/deploy-audio.sh $uniquePrefixString &

# Text Microservice Deploy

time ./scripts/deploy-text.sh $uniquePrefixString &

wait

# Deploy Proxy
time ./scripts/deploy-proxy.sh $uniquePrefixString &

# Deploy Web
time ./scripts/deploy-web.sh $uniquePrefixString &

wait 

echo "$C$D Deployment complete for $uniquePrefixString!$NC"