#!/bin/bash

sftp -vv -oKexAlgorithms=diffie-hellman-group14-sha1 -oCiphers=3des-cbc foo@localhost

