Azure Functions local runtime startup looks only at local.settings.host (not config built in code at startup)

As of this time, the Azure Functions runtime v4/net6/7-isolated & v3/net5-isolated, in Azure, does not pick up a user-assigned managed identity.
Create a system-assigned managed identity for the function app service, 
and add to groups that get RBAC access to azure resources (dev-kv-read, dev-appconfig-read, etc)

https://docs.microsoft.com/en-us/azure/azure-functions/security-concepts
In order to use keyvault (instead of blob storage) to manage keys - assign the function app managed identity - secrets Get, Set, List, and Delete
For now using RBAC - 'Key Vault Secrets Officer' seems to fit

https://docs.microsoft.com/en-us/azure/azure-functions/dotnet-isolated-process-guide

run outside VS with --verbose to debug
install latest azure functions sdk core tools - https://github.com/Azure/azure-functions-core-tools 
func host start --verbose
