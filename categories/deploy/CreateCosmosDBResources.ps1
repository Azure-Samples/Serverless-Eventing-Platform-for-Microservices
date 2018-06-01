param(
    [Parameter(Mandatory=$true)]
    [string]
    $ResourceGroupName,

    [Parameter(Mandatory=$true)]
    [string]
    $DatabaseName,

    [Parameter(Mandatory=$true)]
    [string]
    $CollectionName,

    [Parameter(Mandatory=$true)]
    [string]
    $PartitionKeyPath,

    [int]
    $Throughput = 1000
)

# Find the Cosmos DB account
Write-Host 'Finding Cosmos DB account...'
$accountsJson = az cosmosdb list `
    --resource-group $ResourceGroupName `
    --query [].name `
    --output tsv `
    2>&1
if ($LASTEXITCODE -eq 0)
{
    $cosmosDBAccountName = $accountsJson
    Write-Host "Found account $cosmosDBAccountName"
}
else
{
    throw $accountsJson
}

# Create the database if it doesn't already exist
Write-Host 'Checking if database exists...'
$databaseExists = az cosmosdb database exists `
	--name $cosmosDBAccountName `
	--db-name $DatabaseName `
	--resource-group $ResourceGroupName
if ($databaseExists -eq "false")
{
    Write-Host "Creating database $DatabaseName..."
    az cosmosdb database create `
        --name $CosmosDBAccountName `
        --db-name $DatabaseName `
        --resource-group $ResourceGroupName
}

# Create the collection if it doesn't already exist
Write-Host 'Checking if collection exists...'
$collectionExists = az cosmosdb collection exists `
	--name $cosmosDBAccountName `
    --collection-name $CollectionName `
	--db-name $DatabaseName `
	--resource-group $ResourceGroupName
if ($collectionExists -eq "false")
{
    Write-Host "Creating collection $CollectionName..."
    az cosmosdb collection create `
        --name $cosmosDBAccountName `
        --collection-name $CollectionName `
        --db-name $DatabaseName `
        --resource-group $ResourceGroupName `
        --partition-key-path $PartitionKeyPath `
        --throughput $Throughput
}
