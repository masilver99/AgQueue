# Development overview of AgQueue #

This document serves as a collection of development notes and will grow substantially and become mroe organized over time.

## Project Descriptions: ##

AgQueue.Library - The API library performing queue and message functions.  This is the core of the project.  This library will be exposed via REST or a TCP server.  It can use any storage library, although, currently only SQLite is supported.

AgQueue.Library.Tests - unit and integration tests for the Library project

AgQueue.TcpClient - A TCP client to communicate with AgQueue.  This will be used to 

AgQueue - Project for Server process hosting the AgQueue.Library.  This will eventually be a TCP and REST server, configurable by a config file.

AgQueue.Common - Will eventually contain interfaces for the client libraries

## Expected Development Timeline: ##

Complete AgQueue.Library project.  This should allow for the following functions:

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

## Design Decisions ##

### Primary Keys ###

Primary keys are int64s. They are created by the application, instead of the database or storage mechanism.  This allows for handling of storage that doesn't provide a way to increment the primary key.    

While using something like a GUID would come with certain advantages, I've seen problems with using them.  Many databases don't index them effeciently when they aren't sequential. Performance could be impacted with a great deal of lookups by the primary key.  I've seen SQLServer suffer when using GUIDs as the primary key.

Not using GUID has some serious tradeoffs.  Only one process can use the storage, since multiple processes would cause primary key conflicts.

I'm not opposed to revisiting the use of GUID primary keys, but for the time being Int64's should be adequate.

## Notes ##

Queue names are case sensitive

## Transactions ##

In the code, transactions can be confusing.  Externally, people using the library, will only have one type of transaction, the queue transaction.  Internally, to the programmer, in the storage code, there are two types.  One is the internal transaction, used by the database, i.e. the database transaction.  This one is maintained by the database and used to group SQL statements that must all complete to be valid.  There is also a queue transaction which is maintained by the Internal API.  This is stored in the Transaction table and used for the handling of queue messages.  This is probably the trickiest part of the code base since all the queue transactions must be handled internally.

## Custom Storage classes ##

When creating a custom storage library to use a database other than Sqlite, use the StorageSqlite class for further documentation and as an example.
