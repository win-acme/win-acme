---
sidebar: reference
---

# Route 53
Create the record in Amazon Route53

{% include plugin-seperate.md %}

## Setup
This requires either a user or an IAM role with the following permissions on the zone: 
`route53:GetChange`, `route53:ListHostedZones` and `route53:ChangeResourceRecordSets`

## Unattended 
- User:
`--validation route53 --validationmode dns-01 --route53accesskeyid x --route53secretaccesskey ***`
- IAM  role:
`--validation route53 --validationmode dns-01 --route53iamrole x`