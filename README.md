# NWorkQueue
A simple, opinionated, transactional work queue

Purpose:

To reliably deliver messages to a requestor/subscriber.  Successful messages are only delivered to one REQ/SUB.  In case of failure, the message will be redelivered, unless the retry count is exceeded.

Have you ever used MSMQ, only to be impacted by it's limitations?  For example, not being able to use priority with transactions.

Besides, for simple work queues, MSMQ may be overkill.  Same with RabbitMQ and others.

NWorkQueue Features

* Transactions.  Transactions are mandatory.  Transactions have expiry timespans.  Transactions can be explicitly extended for long running processes.
* Priority.  Messages are returned based on Priority and then by first in.  Priority can be turned off my simply not setting it on each message.
* FIFO or custom ordering based on json message metadata. Beware of orphaned messages!
* Durable.  Messages are always reliably stored to disk.  
* Multiple Queues.  Create as many queues as needed.
* Baked-in Retry.  Messages automatically reenter the queue after a Transaction rollback, BUT you can set a retry delay, eliminating need for a seperate retry queue and preventing poison messages.
* Optional Message History.

