# swish-sftp

An experiment with FTP over SSH (SFTP): how far can I get in a weekend towards having a minimalist working server?

The goal is a server that will accept a connection from the MacOS command-line SFTP client and provide a directory listing and/or file for download.

Note that I am not an expert in SSH - quote the contrary.
One of my main goals is to learn more in depth about how SSH/SFTP work under the hood.
Properly implementing SSH is _hard_.
This code is probably riddled with security vulnerabilies, so please do not use it for anything other than experimentation/learning.


# Project Structure

The bulk of the code _should_ be in the class library: `src/Swish.Sftp`.
A test server, `src/Swish.Sftp.Server` should be pretty minimalist - configure DI/logging/etc and start the server.


# Notes

I used some existing code to help guide my implementation.
Thanks to all the other open-source folks out there that gave me a head start.

* [intothevoid](https://github.com/intothevoid/sshserver) - a project that looks similar to this one? An SSH server in C#.

Most of the code should live in a class library.
My thought is that it should be `netstandard2.0`, but perhaps I'll need `2.2` at some point?
I don't yet understand all the MSFT versioning, maybe I'm just being dense.

* MSFT [version matrix](https://docs.microsoft.com/en-us/dotnet/standard/net-standard#net-implementation-support) - what supports what.
* Using [StyleCop with .NET Core](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/blob/master/documentation/DotNetCli.md)

Logging is fun.
I'm using Serilog in the Server program.

* [Serilog.Extensions.Hosting](https://github.com/serilog/serilog-extensions-hosting)


# Links

* [SFTP specs](https://wiki.filezilla-project.org/SFTP_specifications) - per FileZilla
* [RFC-4253](https://tools.ietf.org/html/rfc4253) - The Secure Shell (SSH) Transport Layer Protocol

