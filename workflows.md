Add Message(s)

1) CreateQueue (if it doesn't already exist)
1) CreateTransaction (optional)
1) AddMessage (called one or more times)
1) CommitTransaction (optional, message(s) will be added to queue) or RollbackTransaction (optional, message(s) won't be added to the queue)

Note: Any messages added in the transaction will not be available until the transaction is commited.

PullMessage

1) CreateTransaction (optional, but will be created automatically with call to PullMessage)
1) PullMessage (message will be temporarily removed from queue, can be called multiple times)
1) CommitTransaction (message will leave queue) or RollbackTransaction (message will return to queue)

PullMessages

1) CreateTransaction (optional, but will be created automatically with call to PullMessage)
1) PullMessages (messages will be temporarily removed from queue, can be called multiple times)
1) CommitTransaction (messages will leave queue) or RollbackTransaction (messages will return to queue)

PeekMessages

1) PeekMessage 

Note: Will return the next message in the queue, but not remove it from the queue.
