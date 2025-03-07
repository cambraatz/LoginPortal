# Establishing/Deploying .NET Application on Apache Linux (CentOS7)
## Hosting Dummy Service
### Establish Project Directory
Create a placeholder web page to establish the kestrel service on Apache for testing static page deployment prior to full integration.

```
cd /var/www
mkdir -p admin/html
cd admin
mkdir log
vim /var/www/admin/index.html
cd ..
chown -R DM_User:DM_User /var/www/admin
chmod -R 755 /var/www/admin
```

**index.html**
```
<h3>Hello world from /admin</h3>
```
### Establish Apache Routing/Redirect
Create the Apache daemon (?) to handle traffic at `admin.domain.com` and direct it to `/var/www/html/admin`.
```
cd /etc/httpd
vim sites-available/admin.conf
ln -s /etc/httpd/sites-available/admin.conf /etc/httpd/sites-enabled/admin.conf
systemctl restart httpd
```

#### Serve Static Pages
**admin.conf**
```
<VirtualHost *:*>
    RequestHeader set "X-Forwarded-Proto" expr=%{REQUEST_SCHEME}s
</VirtualHost>

<VirtualHost *:80>
    #ProxyPreserveHost On
    #ProxyPass / http://127.0.0.1:5500/
    #ProxyPassReverse / http://127.0.0.1:5500/
    #SSLEngine on
    #SSLCertificateFile /etc/ssl/certs/myapp.crt
    #SSLCertificateKeyFile /etc/ssl/private/myapp.key

    ServerName www.admin.tcsservices.com
    ServerAlias *admin.tcsservices.com

    DocumentRoot /var/www/admin
    DirectoryIndex index.html
    <Directory /var/www/admin>
        Require all granted
        #DirectoryIndex index.html
    </Directory>

    ErrorLog /var/www/admin/log/error.log
    CustomLog /var/www/admin/log/requests.log combined
</VirtualHost>
```

#### Serve Application Service
**admin.conf (.NET service)**
```
<VirtualHost *:80>
    RequestHeader set "X-Forwarded-Proto" expr=%{REQUEST_SCHEME}s
</VirtualHost>

<VirtualHost *:80>
    ProxyPreserveHost On
    ProxyPass / http://127.0.0.1:6000/
    ProxyPassReverse / http://127.0.0.1:6000/

    #SSLEngine on
    #SSLCertificateFile /etc/ssl/certs/myapp.crt
    #SSLCertificateKeyFile /etc/ssl/private/myapp.key
    ServerName www.login.tcsservices.com
    ServerAlias login.tcsservices.com *.login.tcsservices.com
    
    DocumentRoot /var/www/login
    
    ErrorLog /var/www/login/log/error.log
    CustomLog /var/www/login/log/requests.log combined
</VirtualHost>
```

**NOTE:** Trouble shoot bugs found upon restarting Apache, typical issues include permissions/ownership, .conf syntax issues, etc.

### Enable HTTPS
Generate and store the SSL credentials, the initial setup creates an encrypted key (more secure) but requires additional passphrase protections at each Apache start up. The second block backsup the old key and creates one without pass phrase protections.

**NOTE:** In production this set up will likely flag for insecure encryption keys/tokens, certified trusted keys should be used in actual deployment.
```
openssl genpkey -algorithm RSA -out /etc/ssl/private/admin.key -aes256
openssl req -new -key /etc/ssl/private/admin.key -out /etc/ssl/private/admin.csr
openssl x509 -req -in /etc/ssl/private/admin.csr -signkey /etc/ssl/private/admin.key -out /etc/ssl/certs/admin.crt -days 365

openssl rsa -in /etc/ssl/private/admin.key -out /etc/ssl/private/admin.key.no_passphrase
mv /etc/ssl/private/admin.key /etc/ssl/private/admin.key.bak
mv /etc/ssl/private/admin.key.no_passphrase /etc/ssl/private/admin.key

chmod 600 /etc/ssl/private/admin.key
chown root:root /etc/ssl/private/admin.key
systemctl restart httpd
```
#### Update Config File
Change the config file to include SSL functionality.

`admin.conf`
```
<VirtualHost *:80>
    ServerName www.login.tcsservices.com
    ServerAlias login.tcsservices.com
    DocumentRoot /var/www/login

    ErrorLog /var/www/login/log/error.log
    CustomLog /var/www/login/log/requests.log combined

    Redirect permanent / https://login.tcsservices.com/
</VirtualHost>

<VirtualHost *:443>
    ServerName www.login.tcsservices.com
    ServerAlias login.tcsservices.com *.login.tcsservices.com
    DocumentRoot /var/www/login

    SSLEngine on
    SSLProxyEngine on
    SSLProxyVerify none

    ProxyPreserveHost On
    ProxyPass / http://127.0.0.1:6000/
    ProxyPassReverse / http://127.0.0.1:6000/
    
    ErrorLog /var/www/login/log/error.log
    CustomLog /var/www/login/log/requests.log combined

    SSLOptions +StrictRequire
    SSLProtocol all -SSLv2 -SSLv3
    SSLCipherSuite HIGH:!aNULL:!MD5:!3DES:!RC4:!LOW:!EXP:!PSK:!SRP
    SSLHonorCipherOrder on

    Include /etc/letsencrypt/options-ssl-apache.conf
    SSLCertificateFile /etc/letsencrypt/live/login.tcsservices.com/cert.pem
    SSLCertificateKeyFile /etc/letsencrypt/live/login.tcsservices.com/privkey.pem
    SSLCertificateChainFile /etc/letsencrypt/live/login.tcsservices.com/chain.pem
</VirtualHost>
```

This self-signed approach seemed to still cause issues with page security. Instead, the self-signed keys are replaced using a free certificate from Let's Encrypt using `certbot`.

### Establish Certbot Encryption Keys
```
sudo yum install epel-release -y
sudo yum install certbot python2-certbot-apache -y

sudo certbot --apache -d login.tcsservices.com -d www.login.tcsservices.com

sudo systemctl restart httpd
```

After restarting, you can ensure the certificates validity with:
```
[admin@server ~]$ sudo certbot certificates
Saving debug log to /var/log/letsencrypt/letsencrypt.log

- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
Found the following certs:
  Certificate Name: login.tcsservices.com
    Serial Number: 3f602e8c1e943a8037331201e535869aa49
    Key Type: RSA
    Domains: login.tcsservices.com www.login.tcsservices.com
    Expiry Date: 2025-05-25 18:26:20+00:00 (VALID: 89 days)
    Certificate Path: /etc/letsencrypt/live/login.tcsservices.com/fullchain.pem
    Private Key Path: /etc/letsencrypt/live/login.tcsservices.com/privkey.pem
    - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
```

#### Auto-Renew SSL Certificate
Let's Encrypt certificates expire every 90 days, so its wise to set up auto-renewal. The following case does so every night at midnight.system
```
sudo crontab -e

> 0 3 * * * certbot renew --quiet && systemctl reload httpd
```

#### Ensure Permission Status
In development, the application user `DM_User` needed to have access to the encryption keys in order for the service to run. It involved easing permissions to both the symlink and hardlink directories for the SSL keys. Without doing so, the service file has no way of providing the encryption we need.

It was a bit round-robin, but the dump below explores all the commands that addressed permissions to get this working:
```
 1104  journalctl -u kestrel-login.service --no-pager --lines=50
 1105  ls /etc/letsencrypt/live/login.tcsservices.com
 1106  ls -l /etc/letsencrypt/live/login.tcsservices.com
 1107  sudo chmod -R 755 /etc/letsencrypt/live/login.tcsservices.com
 1108  ls -l /etc/letsencrypt/live
 1109  systemctl daemon-reload
 1110  systemctl restart kestrel-login.service
 1111  systemctl status kestrel-login.service
 1112  systemctl restart httpd
 1113  systemctl status kestrel-login.service
 1114  journalctl -u kestrel-login.service --no-pager --lines=50
 1115  ls -l /etc/letsencrypt/live/login.tcsservices.com
 1116  journalctl -u kestrel-login.service --no-pager --lines=50
 1117  ls -l /etc/letsencrypt/live/login.tcsservices.com/fullchain.pem
 1118  ls -Z /etc/letsencrypt/live/login.tcsservices.com/fullchain.pem
 1119  sudo -u DM_User cat /etc/letsencrypt/live/login.tcsservices.com/fullchain.pem
 1120  ls -l /etc/letsencrypt/live/login.tcsservices.com/fullchain.pem
 1121  ls -ld /etc/letsencrypt /etc/letsencrypt/live /etc/letsencrypt/live/login.tcsservices.com
 1122  sudo chmod o+rx /etc/letsencrypt/live
 1123  sudo -u DM_User cat /etc/letsencrypt/live/login.tcsservices.com/fullchain.pem
 1124  systemctl restart httpd
 1125  sudo -u DM_User cat /etc/letsencrypt/live/login.tcsservices.com/fullchain.pem
 1126  ls -ld /etc/letsencrypt /etc/letsencrypt/live /etc/letsencrypt/live/login.tcsservices.com
 1127  ls -ld /etc
 1128  sudo chmod o+rx /etc/letsencrypt/live/login.tcsservices.com
 1129  ls -ld /etc/letsencrypt/live/login.tcsservices.com
 1130  ls -ld /etc/letsencrypt/live/login.tcsservices.com/
 1131  ls -l /etc/letsencrypt/live/login.tcsservices.com
 1132  sudo -u DM_User cat /etc/letsencrypt/live/login.tcsservices.com/fullchain.pem
 1133  ls -l /etc/letsencrypt/archive/login.tcsservices.com/
 1134  sudo chmod o+r /etc/letsencrypt/archive/login.tcsservices.com/*
 1135  sudo -u DM_User cat /etc/letsencrypt/live/login.tcsservices.com/fullchain.pem
 1136  ls -l /etc/letsencrypt/archive/login.tcsservices.com/
 1137  sudo chmod o+r /etc/letsencrypt/archive/login.tcsservices.com/privkey1.pem
 1138  ls -l /etc/letsencrypt/archive/login.tcsservices.com/
 1139  sudo -u DM_User cat /etc/letsencrypt/live/login.tcsservices.com/fullchain.pem
 1140  sudo chmod -R o+r /etc/letsencrypt/live
 1141  ls -l /etc/letsencrypt/live/login.tcsservices.com
 1142  sudo -u DM_User cat /etc/letsencrypt/live/login.tcsservices.com/fullchain.pem
 1143  systemctl restart httpd
 1144  sudo -u DM_User cat /etc/letsencrypt/live/login.tcsservices.com/fullchain.pem
 1145  ls -ld /etc /etc/letsencrypt /etc/letsencrypt/live
 1146  ls -l /etc/letsencrypt/archive/login.tcsservices.com/
 1147  chmod -R o+r /etc/letsencrypt/archive/login.tcsservices.com/
 1148  ls -l /etc/letsencrypt/archive/login.tcsservices.com/
 1149  sudo chmod o+r /etc/letsencrypt/archive/login.tcsservices.com/privkey1.pem
 1150  ls -l /etc/letsencrypt/archive/login.tcsservices.com/
 1151  ls -Z /etc/letsencrypt/archive/login.tcsservices.com/
 1152  sudo chmod o+rx /etc/letsencrypt/archive
 1153  sudo chmod o+rx /etc/letsencrypt/archive/login.tcsservices.com
 1154  ls -Z /etc/letsencrypt/archive/login.tcsservices.com/
 1155  ls -l /etc/letsencrypt/archive/login.tcsservices.com/
 1156  chmod -R o+r /etc/letsencrypt/archive/login.tcsservices.com/privkey1.pem
 1157  ls -l /etc/letsencrypt/archive/login.tcsservices.com/
 1158  lsattr /etc/letsencrypt/archive/login.tcsservices.com/privkey1.pem
 1159  mount | grep /etc/letsencrypt
 1160  getfacl /etc/letsencrypt/archive/login.tcsservices.com/privkey1.pem
 1161  ls -ld /etc/letsencrypt /etc/letsencrypt/archive /etc/letsencrypt/archive/login.tcsservices.com
 1162  sudo chmod o+x /etc/letsencrypt/archive
 1163  ls -ld /etc/letsencrypt /etc/letsencrypt/archive /etc/letsencrypt/archive/login.tcsservices.com
 1164  sudo chmod 755 /etc/letsencrypt/archive
 1165  ls -ld /etc/letsencrypt /etc/letsencrypt/archive /etc/letsencrypt/archive/login.tcsservices.com
 1166  ls -ld /etc/letsencrypt/archive/login.tcsservices.com
 1167  sudo chmod 644 /etc/letsencrypt/archive/login.tcsservices.com/fullchain1.pem
 1168  sudo chmod 640 /etc/letsencrypt/archive/login.tcsservices.com/privkey1.pem
 1169  ls -l /etc/letsencrypt/archive/login.tcsservices.com/
 1170  sudo -u DM_User cat /etc/letsencrypt/live/login.tcsservices.com/fullchain.pem
 1171  history
 1172  systemctl daemon-reload
 1173  systemctl restart kestrel-login.service
 1174  systemctl status kestrel-login.service
 1175  journalctl -u kestrel-login.service --no-pager --lines=50
 1176  sudo -u DM_User cat /etc/letsencrypt/live/login.tcsservices.com/privkey.pem
 1177  sudo -u DM_User cat /etc/letsencrypt/live/login.tcsservices.com/fullchain.pem
 1178  ls -l /etc/letsencrypt/live/login.tcsservices.com/
 1179  ls -l /etc/letsencrypt/archive/login.tcsservices.com/
 1180  sudo chmod 644 /etc/letsencrypt/live/login.tcsservices.com/privkey.pem
 1181  ls -l /etc/letsencrypt/archive/login.tcsservices.com/
 1182  sudo -u DM_User cat /etc/letsencrypt/live/login.tcsservices.com/privkey.pem
 1183  systemctl daemon-reload
 1184  systemctl restart kestrel-login.service
 1185  systemctl status kestrel-login.service
 1186  ps aux | grep kestrel-login
 1187  curl -v http://127.0.0.1:6000/
 1188  curl -v https://127.0.0.1:6000/
 1189  vim /etc/httpd/sites-available/login.conf
 1190  systemctl daemon-reload
 1191  systemctl restart kestrel-login.service
 1192  systemctl status kestrel-login.service
 1193  systemctl restart httpd
 1194  systemctl status kestrel-login.service
 1195  vim /etc/httpd/sites-available/login.conf
```
When establishing next module, go through commands above and select only those that are critical to the process. It is also worth looking into to establish security on the keys once again, likely by adding DM_User to the ownership group.

The process for getting the existing login service hosted and functional over HTTPS is seen in the lines below:
```
936  sudo certbot --apache -d deliverymanager.tcsservices.com -d www.deliverymanager.tcsservices.com
  937  sudo certbot certificates
  938  cat /etc/crontab
  939  ls -l /etc/cron.d/
  940  ps aux | grep cron
  941  crontab -l
  942  sudo certbot renew --dry-run
  943  crontab -l
  944  sudo certbot certificates
  945  ls -l /etc/letsencrypt/live
  946  ls -l /etc/letsencrypt/live/deliverymanager.tcsservices.com
  947  ls -l /etc/letsencrypt/live/login.tcsservices.com
  948  ls -l /etc/letsencrypt/archive/login.tcsservices.com/
  949  ls -l /etc/letsencrypt/archive/deliverymanager.tcsservices.com/
  950  sudo chmod 640 /etc/letsencrypt/archive/deliverymanager.tcsservices.com/privkey1.pem
  951  ls -l /etc/letsencrypt/archive/deliverymanager.tcsservices.com/
  952  sudo chmod 644 /etc/letsencrypt/archive/deliverymanager.tcsservices.com/privkey1.pem
  953  ls -l /etc/letsencrypt/archive/deliverymanager.tcsservices.com/
  954  sudo chmod 640 /etc/letsencrypt/archive/deliverymanager.tcsservices.com/privkey1.pem
  955  sudo chmod 644 /etc/letsencrypt/archive/deliverymanager.tcsservices.com/privkey1.pem
  956  sudo -u DM_User cat /etc/letsencrypt/live/deliverymanager.tcsservices.com/privkey.pem
  957  cd /etc/systemd/system
  958  ls
  959  cat kestrel-login.service
  960  vim kestrel-deliverymanager.service
  961  systemctl restart kestrel-deliverymanager.service
  962  systemctl daemon reload
  963  systemctl daemon-reload
  964  systemctl restart kestrel-deliverymanager.service
  965  systemctl status kestrel-deliverymanager.service
  966  cd /etc/httpd
  967  ls
  968  cd sites-available
  969  ls
  970  cat deliverymanager.conf
  971  vim deliverymanager.conf
  972  ls
  973  cat deliverymanager-le-ssl.conf
  974  cat login.conf
  975  mv deliverymanager-le-ssl.conf deliverymanager-le-ssl.conf.bak
  976  ls
  977  vim deliverymanager.conf
  978  ls
  979  cp deliverymanager.conf deliverymanager.conf.bak
  980  ls
  981  vim deliverymanager.conf
  982  systemctl restart httpd
  983  vim deliverymanager.conf
  984  systemctl restart httpd
  985  systemctl status httpd.service
  986  systemctl status httpd.service -l
  987  vim /etc/httpd/conf/httpd.conf
  988  systemctl restart httpd
  989  systemctl status httpd.service -l
  990  history
  991  systemctl status kestrel-deliverymanager.service
  992  systemctl status kestrel-login.service
  993  sudo /usr/bin/deploy_app.sh
  994  ls
  995  exit
  996  history
  997  sudo /usr/bin/deploy_app.sh
  998  exit
  999  history
```

NOTE: common debug step is to ensure .NET program.cs file specifies the same port that has been assigned in the steps above.

**directory permissions**
```
[root@server login]# ls -l /etc/letsencrypt/archive/login.tcsservices.com/
total 16
-rw-r--r--. 1 root root 1822 Feb 24 13:24 cert1.pem
-rw-r--r--. 1 root root 1801 Feb 24 13:24 chain1.pem
-rw-r--r--. 1 root root 3623 Feb 24 13:24 fullchain1.pem
-rw-r--r--. 1 root root 1704 Feb 24 13:24 privkey1.pem

[root@server login]# ls -l /etc/letsencrypt/live/login.tcsservices.com/
total 4
lrwxrwxrwx. 1 root root  45 Feb 24 13:24 cert.pem -> ../../archive/login.tcsservices.com/cert1.pem
lrwxrwxrwx. 1 root root  46 Feb 24 13:24 chain.pem -> ../../archive/login.tcsservices.com/chain1.pem
lrwxrwxrwx. 1 root root  50 Feb 24 13:24 fullchain.pem -> ../../archive/login.tcsservices.com/fullchain1.pem
lrwxrwxrwx. 1 root root  48 Feb 24 13:24 privkey.pem -> ../../archive/login.tcsservices.com/privkey1.pem
-rwxr-xr-x. 1 root root 692 Feb 24 13:24 README
```

### Create the Service
Establish a Linux service that 
```
cd /etc/systemd/system
vim kestrel-admin.service

chown -R DM_User:DM_User kestrel-admin.service
chmod -R 755 kestrel-admin.service

systemctl enable kestrel-admin.service
systemctl start kestrel-admin.service
systemctl status kestrel-admin.service
```

`[root@server system]# cat kestrel-login.service`
```
[Unit]
Description=.NET WebAPI Login App running on CentOS7

[Service]
WorkingDirectory=/var/www/login
ExecStart=/usr/share/dotnet/dotnet /var/www/login/LoginPortal.Server.dll
Restart=always
# Restart service after 10 seconds if the dotnet service crashes:
RestartSec=10
KillSignal=SIGINT

StandardOutput=journal
StandardError=journal
SyslogIdentifier=dotnet-DM

User=DM_User
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=DOTNET_CLI_HOME=/tmp
Environment=ASPNETCORE_URLS=https://0.0.0.0:6000
Environment=ASPNETCORE_Kestrel__Certificates__Default__Path=/etc/letsencrypt/live/login.tcsservices.com/fullchain.pem
Environment=ASPNETCORE_Kestrel__Certificates__Default__KeyPath=/etc/letsencrypt/live/login.tcsservices.com/privkey.pem


[Install]
WantedBy=multi-user.target
```

# End of Routine Notes
List of commands to establish **Login** Module:
```
562  cd /var/www
  563  ls
  564  mkdir -p login/html
  565  ls
  566  cd login
  567  ls
  568  mkdir log
  569  ls
  570  cd ..
  571  ls
  572  ls -ll
  573  chown -R DM_User:DM_User /var/www/login
  574  ls -ll
  575  cd login
  576  ls
  577  ls -ll
  578  cd ..
  579  ls
  580  chmod -R 755 /var/www/login
  581  ls -ll
  582  vim /var/www/login/html/index.html
  583  ls
  584  cd /etc/httpd/conf
  585  ls
  586  vim httpd.conf
  587  ls
  588  cd ..
  589  ls
  590  cd sites-available
  591  ls
  592  vim login.conf
  593  ls
  594  touch deliverymanager.conf
  595  ls
  596  vim deliverymanager.conf
  597  ls
  598  vim login.conf
  599  cd ..
  600  ls
  601  ln -s /etc/httpd/sites-available/login.conf /etc/httpd/sites-enabled/login.conf
  602  cd sites-available
  603  ls -ll
  604  cd ..
  605  ls
  606  cd sites-enabled
  607  ls
  608  cd ..
  609  ls -dZ /var/www/login/log
  610  ls -dZ /var/www/delivermanager/log
  611  ls -dZ /var/www/deliverymanager/log
  612  systemctl restart httpd
  613  cd /var/www
  614  ls
  615  cd login
  616  ls
  617  cd /etc/httpd/conf
  618  ls
  619  cd ..
  620  ls
  621  cd sites-available
  622  ls
  623  vim login.conf
  624  cd ..
  625  ls
  626  cd /etc/httpd/conf
  627  ls
  628  vim httpd.conf
  629  ls
  630  cd ..
  631  ls
  632  cd sites-available
  633  ls
  634  vim deliverymanager.conf
  635  ls
  636  vim login.conf
  637  ls
  638  vim deliverymanager.conf
  639  systemctl restart httpd
  640  vim deliverymanager.conf
  641  systemctl restart httpd
  642  vim deliverymanager.conf
  643  systemctl restart httpd
  644  systemctl restart apache2
  645  systemctl restart apache
  646  ls
  647  vim login.conf
  648  systemctl restart httpd
  649  cd /var/www
  650  ls
  651  cd login
  652  ls
  653  cd html
  654  ls
  655  cd ..
  656  ls
  657  mv /html/index.html index.html
  658  mv ./html/index.html index.html
  659  ls
  660  ls -ll
  661  chown -R DM_User:DM_User index.html
  662  chmod -R 755 /var/www/login/index.html
  663  ls -ll
  664  cd ..
  665  ls -ll
  666  systemctl restart httpd
  667  tail -f /var/www/login/log/error.log
  668  ls
  669  cd ..
  670  ls
  671  cd /etc/httpd
  672  ls
  673  cd sites-available
  674  ls
  675  vim login.conf
  676  systemctl restart httpd
  677  vim login.conf
  678  systemctl restart httpd
  679  exit
  680  cd /etc/httpd
  681  ls
  682  cd sites-available
  683  ls
  684  vim login.conf
  685  systemctl restart httpd
  686  vim login.conf
  687  systemctl restart httpd
  688  exit
  689  cd /etc/systemd/system
  690  ls
  691  nano kestrel-login.service
  692  ls
  693  vim kestrel-login.service
  694  systemctl enable kestrel-login.service
  695  systemctl start kestrel-login.service
  696  systemctl status kestrel-login.service
  697  ls
  698  cd ..
  699  cd /var/www
  700  ls
  701  cd ..
  702  ls
  703  cd ..
  704  ls
  705  cd bin
  706  ls
  707  cd bash
  708  vim deploy_login.sh
  709  ls
  710  ls -ll
  711  ls -ll deploy*
  712  chmod 755 deploy_login.sh
  713  ls -ll deploy*
  714  cd /var/www
  715  ls
  716  cd login
  717  history
  718  sudo /usr/bin/deploy_login.sh
  719  history
  720  systemctl enable kestrel-login.service
  721  systemctl start kestrel-login.service
  722  systemctl status kestrel-login.service
  723  ls
  724  pwd
  725  systemctl restart httpd
  726  systemctl status kestrel-login.service
  727  systemctl restart kestrel-login.service
  728  systemctl status kestrel-login.service
  729  cd /etc/systemd/system
  730  ls
  731  ls -ll kestrel*
  732  chown -R DM_User:DM_User kestrel-login.service
  733  chmod -R 755 kestrel-login.service
  734  ls -ll kestrel*
  735  systemctl restart kestrel-login.service
  736  systemctl status kestrel-login.service
  737  cd /var/www
  738  ls
  739  ls -ll
  740  cd login
  741  ls
  742  pwd
  743  cd /etc/systemd/system
  744  ls
  745  vim kestrel-deliverymanager.service
  746  ls
  747  vim kestrel-login.service
  748  ls
  749  cd ..
  750  ls
  751  cd ..
  752  ls
  753  cd httpd
  754  ls
  755  cd logs
  756  ls
  757  tail -100 error_log
  758  ls
  759  tail -100 access_log
  760  ls
  761  cd ..
  762  ls
  763  cd ..
  764  ls
  765  cd ..
  766  ls
  767  cd /var/www
  768  ls
  769  cd login
  770  ls
  771  cd log
  772  ls
  773  man error.log
  774  ls
  775  ls -ll
  776  tail error.log
  777  cd /etc/httpd/sites-available
  778  ls
  779  vim login.conf
  780  systemctl restart kestrel-login.service
  781  systemctl status kestrel-login.service
  782  systemctl restart httpd
  783  systemctl status kestrel-login.service
  784  systemctl restart kestrel-login.service
  785  systemctl status kestrel-login.service
  786  history
  787  tail -f /var/www/login/log/error.log
  788  systemctl status kestrel-login.service
  789  ls
  790  vim login.conf
  791  ls
  792  netstat -tuln | grep 6000
  793  netstat -tuln
  794  ufw status
  795  iptables -L -n -v
  796  sudo iptables -L -n -v | grep 6000
  797  firewall-cmd --list-all
  798  sudo firewall-cmd --query-port=6000/tcp
  799  firewall-cmd --zone=public --add-port=6000/tcp --permanent
  800  firewall-cmd --reload
  801  sudo firewall-cmd --query-port=6000/tcp
  802  systemctl restart httpd
  803  history
  804  systemctl restart kestrel-login.service
  805  systemctl status kestrel-login.service
  806  ls
  807  vim deliverymanager.conf
  808  vim login.conf
  809  ls
  810  history
  811  ls -dZ /var/www/login/log
  812  systemctl disable kestrel-login.service
  813  systemctl stop kestrel-login.service
  814  exit
  815  journalctl -u firewalld
  816  history
  817  systemctl enable kestrel-login.service
  818  systemctl restart kestrel-login.service
  819  systemctl restart httpd
  820  systemctl status kestrel-login.service
  821  sudo journalctl -u kestrel-login.service --since "10 minutes ago"
  822  cd /etc/systemd/system
  823  ls
  824  vim kestrel-login.service
  825  systemctl daemon-reload
  826  systemctl restart kestrel-login.service
  827  systemctl status kestrel-login.service
  828  exit
```