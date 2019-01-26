# Development overview of NWorkQueue #

## Project Descriptions: ##

NWorkQueue.Library - The API library performing queue and message functions.  This is the core of the project.  This library will be exposed via REST or a TCP server.  It can use any storage library, although, currently only SQLite is supported.
NWorkQueue.Library.Tests - unit and integration tests for the Library project
NWorkQueue.TcpClient - A TCP client to communicate with NWorkQueue.  This will be used to 
NWorkQueue - Project for Server process hosting the NWorkQueue.Library.  This will eventually be a TCP and REST server, configurable by a config file.
NWorkQueue.Common - Will eventually contain interfaces for the client libraries

## Expected Development Timeline: ##

Complete NWorkQueue.Library project.  This should allow for the following functions:

 1) Create Transaction
 2) Commit Transaction
 3) Extend Transaction
 4) Rollback Transaction
 5) Add Queue
 6) Delete Queue
 7) Add Message - Adds message to queue (must be in trans)
 8) Pull Message(s) - Pulls next or x count of messages from queue (must be in trans)
 9) Peek Message - Retrieves message without pulling it from queue
10) Delete Message - Deletes a message without marking it as processed
11) GetMessageCount - Get the number of message queued

As each function is completed, unit tests should be created to comfirm functionality

Notes:

Queue names are case sensitive
