External Dependencies:

Visual c++ runtime 9, manually download and install on XP
.NET 2.0 (todo : remove), to manually install on XP







* Generate node and hub certificates *
--------------------------------------
We use PKS12 certs, which include public AND private key.
There are 2 ways to generate a self-signed CA and certificate for a node.
Remember that for security reasons, it is STRONGLY recommended to use or generate ONE root cert that will be used for hub and nodes certificates, 
and to activate cert chain verification in hub configuration.
These 2 methods are only for test purposes, and presented because they are very easy, and will allow you to have a working Backup setup very quickly.
Remember to put the certificate in a place that will not be erased. 

Prerequisite :
---------------
For obvious security reasons, a cert must be readable only by the user who will need it (that is, the user that will run hub or node). 
It is advised to run the following commands under the account that will run Hub or Node processes.


Option 1 : openssl
--------------------
openssl req -x509 -nodes -days 365 -newkey rsa:2048 -keyout certificate.key -out certificate.crt
...answer the questions. The really important part is that "CN" MUST be equal to node hostname (command "hostname" on nix).
openssl pkcs12 -export -out certificate.pfx -inkey certificate.key -in certificate.crt

This will generate a certificate with 1 year validity.

Option 2 : makecert.exe 
------------------------
makecert -r -n "CN=ShBackup Test CA" -sv root.key root.cer
makecert -iv root.key -ic root.cer -eku 1.3.6.1.5.5.7.3.1 -n "CN=linux-ax2g" -p12 certificate.pfx ""
