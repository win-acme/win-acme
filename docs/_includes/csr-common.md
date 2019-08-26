## OCSP Must Staple
This extension can be added to the CSR with the command line option `--ocsp-must-staple`

## Private key reuse
 The option `--reuse-privatekey` can be used to keep using the same 
 private key across different renewals, which can be useful for example with 
 [DANE](https://en.wikipedia.org/wiki/DNS-based_Authentication_of_Named_Entities).