# AADCustomClaimsProvider
Contains a sample Azure AD custom claims provider implemented as an Azure Function

## Prerequisites
Follow the instructions [here](https://learn.microsoft.com/en-us/azure/active-directory/develop/custom-extension-get-started?tabs=azure-portal) to register a custom authentication extension, create the necessary app registrations to support your custom claims provider, and create an Azure function where you can deploy the sample code.

The sample custom claims provider uses the SQL Server sample pubs database uploaded to an Azure SQL database as its data source. The sample pubs database can be found [here](https://github.com/microsoft/sql-server-samples/blob/master/samples/databases/northwind-pubs/readme.md). You will need to create an Azure SQL database and run the pubs creation scripts against that database and then update connection strings in the code accordingly.

Unlike the sample in the tutorial, validation of the JWT token is handled in code rather than depending on the Azure App Service Authentication feature. This allows the code to be tested locally before being deployed to Azure.

Additional supporting documentation is listed below.

- [Custom authentication extensions](https://learn.microsoft.com/en-us/azure/active-directory/develop/custom-extension-overview)
- [Custom claims provider](https://learn.microsoft.com/en-us/azure/active-directory/develop/custom-claims-provider-overview)
