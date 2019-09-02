---
sidebar: reference
---

# Azure DNS 
Create the record in Azure DNS.

{% include plugin-seperate.md %}

## Setup
This assumes you already have your DNS managed in Azure; if not, you'll need to set that up first. If you are 
using the Azure DNS option for validation, you'll need to get certain info from your Azure Tenant, and create 
a service principal for Let's Encrypt to use (you'll only need to create on of these for your entire domain - 
it's basically an account that has authority to create DNS records). 

### Create Azure AD Service Principal Account
Run the following commands in Powershell. You will need to install the AzureRM Powershell module first if 
you don't have it installed already.

`Login-AzureRmAccount`
`$sp = New-AzureRmADServicePrincipal -DisplayName LetsEncrypt -Password "SuperSecretPasswordGoesHere"`

You can change the DisplayName to something else if you like, and you should certainly change the password. 
Keep a note of the password as you'll need it to set up the client in a minute.

You then need to give this Service Principal access to change DNS entries. In the Azure Portal:
* Go to `DNS Zones` > `sub.example.com` > `Access Control (IAM)`
* Click `Add`
* For Role, choose `DNS Zone Contributor`
* Assign access to `Azure AD user, group, or application`
* Select `LetsEncrypt` (or whatever you called your Service Principal above)
* Click `Save`

### Configuring the plugin
* Run `wacs.exe`, and choose which site you want to secure.
* At the section 'How you you like to validate this certificate' choose `Azure DNS`
* For `Tenant ID`: in the Azure Portal: Azure Active Directory > Properties > Directory ID.
* For `Client ID`: in the Azure Portal: Azure Active Directory > App registrations > LetsEncrypt (or whatever you called your Service Principal before), and find the Application ID.
* For the `Secret`: enter the password you created before.
* For the `DNS Subscription ID`: in the Azure Portal: `DNS Zones` > `sub.example.com` > Subscription ID
* For the `DNS Resource Group Name`: the name of the Resource Group your DNS zone is in (you can find this in Azure Portal: DNS zones -> your.dns.zone.net -> Resource Group

### Resources
- [How to: Use Azure PowerShell to create a service principal with a certificate](https://docs.microsoft.com/en-us/azure/active-directory/develop/howto-authenticate-service-principal-powershell)
- [DNS SDK](https://docs.microsoft.com/en-us/azure/dns/dns-sdk)

## Unattended 
`--validationmode dns-01 --validation azure --azuretenantid x --azureclientid x --azuresecret *** --azuresubscriptionid x --azureresourcegroupname x`