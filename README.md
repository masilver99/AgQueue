# NWorkQueue
A simple, opinionated, transactional work queue

## Purpose

To reliably deliver messages to a requestor/subscriber.  Successful messages are only delivered to one REQ/SUB.  In case of failure, the message will be redelivered, after a delay, unless the retry count is exceeded.

Have you ever used MSMQ, only to be impacted by it's limitations?  For example, not being able to use priority with transactions.  This aims to allieviate those pain points.

Besides, for simple work queues, MSMQ may be overkill.  Same with RabbitMQ and others.

[Development Overview](dev_overview.md)

### Example

A classic example of when to use a work queue is sending email from a website.  When your users request a password change or any other activity that will send an email, you don't want their browsing experience to slow down while the email is sent, so you place a message into a queue, which is near instant.  In the background or on another machine, the message is picked up and and email is sent out.

## NWorkQueue Features

* Transactions.  Transactions are mandatory.  Transactions have expiry timespans.  Transactions can be explicitly extended for long running processes.
* Priority.  Messages are returned based on Priority and then by first in.  Priority can be turned off my simply not setting it on each message.
* Durable.  Messages are always reliably stored to disk.  
* Multiple Queues.  Create as many queues as needed.
* Baked-in Retry.  Messages automatically reenter the queue after a Transaction rollback, BUT you can set a retry delay, eliminating need for a seperate retry queue and preventing poison messages.
* Optional message metadata.  Allows storage of custom data relating to the message.  
* FIFO or custom ordering based on json message metadata. Beware of orphaned messages!
* Optional Message History.
* Optional Correlation ID (integer) and/or Group string to assist with grouping of messages to pull.

## Additional Details

NWorkQueue consists of a server process that contains communication threads.  Currently, two communication threads/libraries are planned.  A TCP socket and REST library.  The socket library will have a client library to wrap the socket calls.

NWorkQueue uses SQLite for storage.  While not as fast as using in-memory containers, it's HIGHLY resilient to machine failures, i.e. a spurious reboot won't cause your queue to disappear.  At a future date, we may offer different storage options, including memory-only, for those that don't want durable storage.

NWorkQueue is built with C# in .NET Core, i.e. standard 2.0.  This means it should run on Windows, Linux and Mac.  

We'll try to post benchmarks once the project is complete, but the goal is more durability and resilency focused than raw speed, however constant speed optimaizations are being made.
