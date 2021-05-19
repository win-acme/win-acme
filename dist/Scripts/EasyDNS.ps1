<#
.SYNOPSIS
Add or remove a DNS TXT record to EasyDNS
.DESCRIPTION
Note that this script is intended to be run via the install script plugin from win-acme via the batch script wrapper. 
As such, we use positional parameters to avoid issues with using a dash in the cmd line. 

This script was copied and modified from the Posh-ACME repository.  
Please reference their license terms for use/modification:  https://github.com/rmbolger/Posh-ACME/blob/main/LICENSE

Credit for the original script goes to RMBolger, Thanks!


.PARAMETER RecordName
The fully qualified name of the TXT record.

.PARAMETER TxtValue
The value of the TXT record.

.PARAMETER EDToken
The EasyDNS API Token.

.PARAMETER EDKey
The EasyDNS API Key.

.PARAMETER EDUseSandbox
If specified, the plugin runs against the EasyDNS Sandbox environment instead of the Live environment.

.PARAMETER ExtraParams
This parameter can be ignored and is only used to prevent errors when splatting with more parameters than this function supports.


.EXAMPLE 

EasyDNS.ps1 create {RecordName} {Token} EDToken EDKey

EasyDNS.ps1 delete {RecordName} {Token} EDToken EDKey

.NOTES

#>
param(
	[string]$Task,
	[string]$RecordName,
	[string]$TxtValue,
	[string]$EDToken,
	[string]$EDKey
)

Function Add-DnsTxt {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory,Position=0)]
        [string]$RecordName,
        [Parameter(Mandatory,Position=1)]
        [string]$TxtValue,
        [Parameter(Mandatory,Position=2)]
        [string]$EDToken,
        [Parameter(Mandatory,Position=3)]
        [string]$EDKey,
        [switch]$EDUseSandbox,
        [Parameter(ValueFromRemainingArguments)]
        $ExtraParams
    )

    # set the API base
    $apiBase = if ($EDUseSandbox) { "https://sandbox.rest.easydns.net" } else { "https://rest.easydns.net" }

    # create the basic auth header
    $encodedCreds = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes("$($EDToken):$($EDKey)"))
    $Headers = @{ Authorization = "Basic $encodedCreds" }

    # find the domain/zone associated with this record
    $pieces = $RecordName.Split('.')
    for ($i=0; $i -lt ($pieces.Count-1); $i++) {
        $zoneTest = $pieces[$i..($pieces.Count-1)] -join '.'
        try {
            $Records = Invoke-RestMethod "$apiBase/zones/records/all/$($zoneTest)?format=json" `
                -ContentType 'application/json' -Headers $Headers -Method GET
        } catch { continue }
        $domain = $zoneTest
        Write-Verbose "Found $domain zone"
        break
    }
    if (-not $domain) { throw "Unable to find zone for $RecordName" }

    # grab the relative portion of the fqdn
    $recShort = ($RecordName -ireplace [regex]::Escape($domain), [string]::Empty).TrimEnd('.')

    # check for existing record
    $rec = $Records.data | Where-Object { $_.type -eq 'TXT' -and $_.host -eq $recShort -and $_.rData -eq $TxtValue }
    if ($rec) {
        Write-Debug "Record $RecordName already contains $TxtValue. Nothing to do."
    } else {
        # add it
        Write-Verbose "Adding a TXT record for $RecordName with value $TxtValue"

        $body = @{
            host = $recShort
            domain = $domain
            ttl = 0
            prio = 0
            type = "txt"
            rdata = $TxtValue
        } | ConvertTo-Json
        Write-Debug $body

        Invoke-RestMethod "$apiBase/zones/records/add/$domain/txt?format=json" -Method Put `
            -Body $body -ContentType 'application/json' -Headers $Headers
    }

    <#
    .SYNOPSIS
        Add a DNS TXT record to EasyDNS.

    .DESCRIPTION
        Add a DNS TXT record to EasyDNS.

    .PARAMETER RecordName
        The fully qualified name of the TXT record.

    .PARAMETER TxtValue
        The value of the TXT record.

    .PARAMETER EDToken
        The EasyDNS API Token.

    .PARAMETER EDKey
        The EasyDNS API Key.

    .PARAMETER EDUseSandbox
        If specified, the plugin runs against the Sandbox environment instead of the Live environment.

    .PARAMETER ExtraParams
        This parameter can be ignored and is only used to prevent errors when splatting with more parameters than this function supports.

    .EXAMPLE
        Add-DnsTxt '_acme-challenge.example.com' 'txtvalue' -EDToken 'xxxxxxxx' -EDKey 'xxxxxxxx'

        Adds a TXT record for the specified site with the specified value.
    #>
}

Function Remove-DnsTxt {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory,Position=0)]
        [string]$RecordName,
        [Parameter(Mandatory,Position=1)]
        [string]$TxtValue,
        [Parameter(Mandatory,Position=2)]
        [string]$EDToken,
        [Parameter(Mandatory,Position=3)]
        [string]$EDKey,
        [switch]$EDUseSandbox,
        [Parameter(ValueFromRemainingArguments)]
        $ExtraParams
    )

    # set the API base
    $apiBase = if ($EDUseSandbox) { "https://sandbox.rest.easydns.net" } else { "https://rest.easydns.net" }

    # create the basic auth header
    $encodedCreds = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes("$($EDToken):$($EDKey)"))
    $Headers = @{ Authorization = "Basic $encodedCreds" }

    # find the domain/zone associated with this record
    $pieces = $RecordName.Split('.')
    for ($i=0; $i -lt ($pieces.Count-1); $i++) {
        $zoneTest = $pieces[$i..($pieces.Count-1)] -join '.'
        try {
            $Records = Invoke-RestMethod "$apiBase/zones/records/all/$($zoneTest)?format=json" `
                -ContentType 'application/json' -Headers $Headers -Method GET
        } catch { continue }
        $domain = $zoneTest
        Write-Verbose "Found $domain zone"
        break
    }
    if (-not $domain) { throw "Unable to find zone for $RecordName" }

    # grab the relative portion of the fqdn
    $recShort = ($RecordName -ireplace [regex]::Escape($domain), [string]::Empty).TrimEnd('.')

    # check for existing record
    $rec = $Records.data | Where-Object { $_.type -eq 'TXT' -and $_.host -eq $recShort -and $_.rData -eq $TxtValue }
    if ($rec) {
        # remove it
        Write-Verbose "Removing TXT record for $RecordName with value $TxtValue"
        Invoke-RestMethod "$apiBase/zones/records/$domain/$($rec.id)?format=json" -Method Delete `
            -ContentType 'application/json' -Headers $Headers
    } else {
        Write-Debug "Record $RecordName with value $TxtValue doesn't exist. Nothing to do."
    }

    <#
    .SYNOPSIS
        Remove a DNS TXT record to EasyDNS.

    .DESCRIPTION
        Remove a DNS TXT record to EasyDNS.

    .PARAMETER RecordName
        The fully qualified name of the TXT record.

    .PARAMETER TxtValue
        The value of the TXT record.

    .PARAMETER EDToken
        The EasyDNS API Token.

    .PARAMETER EDKey
        The EasyDNS API Key.

    .PARAMETER EDUseSandbox
        If specified, the plugin runs against the Sandbox environment instead of the Live environment.

    .PARAMETER ExtraParams
        This parameter can be ignored and is only used to prevent errors when splatting with more parameters than this function supports.

    .EXAMPLE
        Remove-DnsTxt '_acme-challenge.example.com' 'txtvalue' -EDToken 'xxxxxxxx' -EDKey 'xxxxxxxx'

        Removes a TXT record for the specified site with the specified value.
    #>
}

function Save-DnsTxt {
    [CmdletBinding()]
    param(
        [Parameter(ValueFromRemainingArguments)]
        $ExtraParams
    )
    <#
    .SYNOPSIS
        Not required.

    .DESCRIPTION
        This provider does not require calling this function to commit changes to DNS records.

    .PARAMETER ExtraParams
        This parameter can be ignored and is only used to prevent errors when splatting with more parameters than this function supports.
    #>
}

if ($Task -eq 'create'){
	Add-DnsTxt $RecordName $TxtValue $EDToken $EDKey
}

if ($Task -eq 'delete'){
	Remove-DnsTxt $RecordName $TxtValue $EDToken $EDKey
}

############################
# Helper Functions
############################

# API Docs
# http://docs.sandbox.rest.easydns.net/
# http://sandbox.rest.easydns.net:3000/
