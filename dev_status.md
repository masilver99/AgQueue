2/25/2020 

Originally, this project was going to offer a TCP and REST server for interacting with the queue.  

Instead, those two servers are going to be consolodated into a single gRPC server.  While not as fast as TPC sockets and not as compatible as REST, gRPC is now robust enough to server as the primary means of accessing the queue going forward.

Active Goals:
1) Finish up gRPC server apis
2) Create integration tests to hit against gRPC server
3) Rework internal api/repository to work better with gRPC calls
