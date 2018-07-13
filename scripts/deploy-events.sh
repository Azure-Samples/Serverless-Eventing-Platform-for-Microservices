#!/bin/bash

HOME=`pwd`

C="\033[1;35m"
D=`date +%Y-%m-%d-%H:%M:%S`
NC='\033[0m'

# Creating Event Grid Topic

echo -e "$C$D Creating event grid resource group for $1.$NC"
time az group create -n $1"-events" -l westus2
echo -e "$C$D Created event grid resource group for $1.$NC"

echo -e "$C$D Creating event grid topic for $1.$NC"
EVENT_GRID_TOPIC_NAME=$1"-events-topic"
time az group deployment create -g $1"-events" --template-file $HOME/events/deploy/template.json --mode Complete --parameters uniqueResourceNamePrefix=$1
echo -e "$C$D Created event grid topic for $1.$NC"

time sleep 2

echo -e "$C$D Completed events deployment for $1.$NC"
