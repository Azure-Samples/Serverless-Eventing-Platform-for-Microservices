param(
    [Parameter(Mandatory=$true)]
    [string]
    $UniqueResourceNamePrefix
)

$ResourceGroupName = "$UniqueResourceNamePrefix-audio"
$StorageAccountName = "$ResourceGroupName-blob"
$CollectionName = "audio"

az storage container create --account-name $StorageAccountName  --name $CollectionName
az storage cors clear --account-name $StorageAccountName --services b
az storage cors add --account-name $StorageAccountName --services b --methods POST GET PUT --origins * --allowed-headers * --exposed-headers *
