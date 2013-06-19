CONSOLE APPLICATION : RavenBench Project Overview
====================================================
## Overview 
Test program to verify generate load on RavenDB instance.

This test clients purpose is to overload a RavenDB server to the point it starts to randomly close client connections.

RavenBench uses Parallel.Foreach to attempt quickly read then write or just read a RavenDB 1,000,000 times.

## Instructions

Initially run only one instance of RavenBench to create the 1,000,000 user documents.

Once the user Documents have been created, run as many RavenBench as you like.

RavenBench will execute 1M:
1. Read, permute, writes
2. Reads
as fast as possible.

Hitting a key while the program is running will write all the current errors to a text file with the name "Errors_<ProcessId>.txt" then close.