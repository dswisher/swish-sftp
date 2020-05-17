#!/bin/bash

sftp -vv -oUserKnownHostsFile=/dev/null -oKexAlgorithms=diffie-hellman-group14-sha1 -oCiphers=3des-cbc foo@localhost

