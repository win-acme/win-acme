Import-module dnsserver
$computername = "localhost"
$action = $args[0]
$zone = $args[1]
$name = $args[2].Replace($zone,"").Trim(".")
if($action -eq "create") {
	$text = $args[3]
	Add-DnsServerResourceRecord -TXT -Computername $computername -ZoneName $zone -Name $name -DescriptiveText $text
}elseif($action -eq "delete") {
	Remove-DnsServerResourceRecord -RRType "Txt" -Computername $computername -ZoneName $zone -Name $name -Force
}