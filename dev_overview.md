# Development overview of NWorkQueue #

This document serves as a collection of development notes and will grow substantially and become mroe organized over time.

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

## Developer Expectations ##

Code should follow style cop recommendations.  The style cop analyzer is checked in with each project.  

There should be no warnings in a Pull Requests.

All public methods and properties should be well documented using the documentaion comment: ///

## Notes ##

Queue names are case sensitive

## Transactions ##

In the code, transactions can be confusing.  Externally, people using the library, will only have one type of transaction, the queue transaction.  Internally, to the programmer, in the storage code, there are two types.  One is the internal transaction, used by the database, i.e. the database transaction.  This one is maintained by the database and used to group SQL statements that must all complete to be valid.  There is also a queue transaction which is maintained by the Internal API.  This is stored in the Transaction table and used for the handling of queue messages.  This is probably the trickiest part of the code base since all the queue transactions must be handled internally.

## Custom Storage classes ##

When creating a custom storage library to use a database other than Sqlite, use the StorageSqlite class for further documentation and as an example.
