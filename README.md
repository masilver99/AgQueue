# AgQueue
A simple, opinionated, transactional work queue

![.NET Core](https://github.com/masilver99/AgQueue/workflows/.NET%20Core/badge.svg?branch=master)

AgQueue is a server process that can use almost any database as it's back end.  It essentially turns any supported database into a full fledged work queue.

AgQueue runs between the clients and the database and automatically provides features of an advanced work queue or messaging system. 

## Purpose

To reliably deliver messages to a requestor/subscriber.  Successful messages are only delivered to one REQ/SUB.  In case of failure, the message will be redelivered, after a delay, unless the retry count is exceeded.

Have you ever used MSMQ, only to be impacted by it's limitations?  For example, not being able to use priority with transactions.  This aims to allieviate those pain points.

Besides, for simple work queues, MSMQ may be overkill.  Same with RabbitMQ and others.

[Development Overview](dev_overview.md)

[Usage Workflows](workflows.md)

[Development Status](dev_status.md)

### Example

A classic example of when to use a work queue is sending email from a website.  When your users request a password change or any other activity that will send an email, you don't want their browsing experience to slow down while the email is sent, so you place a message into a queue, which is near instant.  In the background or on another machine, the message is picked up and and email is sent out.

## AgQueue Features

* Transactions.  Transactions are mandatory.  Transactions have expiry timespans.  Transactions can be explicitly extended for long running processes.
* Priority.  Messages are returned based on Priority and then by first in.  Priority can be turned off my simply not setting it on each message.
* Durable.  Messages are always reliably stored to disk.  
* Multiple Queues.  Create as many queues as needed.
* Baked-in Retry.  Messages automatically reenter the queue after a Transaction rollback, BUT you can set a retry delay, eliminating need for a seperate retry queue and preventing poison messages.
* Optional message metadata.  Allows storage of custom data relating to the message.  
* FIFO or custom ordering based on json message metadata, correlation id, group name or tag value. Beware of orphaned messages!
* Optional Message History.
* Optional Correlation ID (integer) and/or Group string to assist with grouping of messages to pull.
* Optional Tags.  Tags can be applied to any message.  A tag can also have a value, for example, year=2019.  There can be multiple tags on a message.  This is a more effeicient way of organizing and pulling messages than using the metadata.

## Additional Details

AgQueue consists of a server process that contains a gRPC communication server and a gRPC client for communicating with the queue.  

AgQueue uses SQLite for storage.  While not as fast as using in-memory containers, it's HIGHLY resilient to machine failures, i.e. a spurious reboot won't cause your queue to disappear.  At a future date, we may offer different storage options, including memory-only, for those that don't want durable storage.

AgQueue is built with C# in .NET 9.  This means it should run on Windows, Linux and Mac.  

We'll post benchmarks once the project is complete, but the goal is more durability and resilency than raw speed.
