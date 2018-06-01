param ($SubscriptionId, $ResourceGroupName)

$seedString = -join($SubscriptionId, $ResourceGroupName)
$hashArray = (New-Object System.Security.Cryptography.SHA512Managed).ComputeHash($seedString.ToCharArray())
$hashedString = -join ($hashArray[1..13] | ForEach-Object { [char]($_ % 26 + [byte][char]'a') })

Write-Output "##vso[task.setvariable variable=UniqueResourceNameSuffix;]$hashedString"
