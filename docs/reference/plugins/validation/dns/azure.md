---
sidebar: reference
---

# Azure DNS 
Create the record in Azure DNS.

{% include plugin-seperate.md %}

## Setup
This assumes you already have your DNS managed in Azure; if not, you'll need to set that up first. If you are 
using the Azure DNS option for validation, you'll need to get certain info from your Azure Tenant, and create 
a service principal for win-acme to use (you'll only need to create on of these - it's basically an account that has authority to create DNS records). 
There are two ways to authenticate with Azure:

#### Create Azure AD Service Principal Account
Use the [Azure CLI](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli-windows?view=azure-cli-latest)
to create an [Azure service principal](https://docs.microsoft.com/en-us/cli/azure/create-an-azure-service-principal-azure-cli?view=azure-cli-latest)

You then need to give this Service Principal access to change DNS entries. In the Azure Portal:
* Go to `DNS Zones` > `sub.example.com` > `Access Control (IAM)`
* Click `Add`
* For Role, choose `DNS Zone Contributor`
* Assign access to `Azure AD user, group, or application`
* Select your Service Principal
* Click `Save`

#### Use a Managed Service Identity
More information [here](https://docs.microsoft.com/en-us/azure/active-directory/managed-identities-azure-resources/overview)

### Configuring the plugin
During setup of the validation the program will ask several questions. 
Here is to answer them with information from the Azure Portal.

* `DNS Subscription ID`: DNS Zones > `sub.example.com` > `Subscription ID`
* `DNS Resource Group Name`: DNS zones > `sub.example.com` > `Resource Group`)

Only when authenticating Service Principal Account:

* `Directory/tenant id`: Azure Active Directory > Properties > `Directory ID`.
* `Application client id`: Azure Active Directory > App registrations > [Service Principal] > `Application ID`.
* `Application client secret`: The password that was generated when you created the Service Principal Account.

### Resources
- [How to: Use Azure PowerShell to create a service principal with a certificate](https://docs.microsoft.com/en-us/azure/active-directory/develop/howto-authenticate-service-principal-powershell)
- [DNS SDK](https://docs.microsoft.com/en-us/azure/dns/dns-sdk)

## Unattended 
#### Service Principal Account
`--validationmode dns-01 --validation azure --azuretenantid x --azureclientid x --azuresecret *** --azuresubscriptionid x --azureresourcegroupname x`
#### Managaged Resource Identity
`--validationmode dns-01 --validation azure --azureusemsi --azuresubscriptionid x --azureresourcegroupname x`