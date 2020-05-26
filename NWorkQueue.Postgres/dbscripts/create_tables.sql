--This is run by the admin in the nworkqueue dataebase

CREATE EXTENSION IF NOT EXISTS citext;

Create table IF NOT EXISTS transactions
(id SERIAL PRIMARY KEY,
 state SMALLINT NOT NULL,
 start_datetime timestamptz NOT NULL,
 expiry_datetimeDateTime timestamptz NOT NULL,
 end_datetime timestamptz,
 end_reason TEXT);

Create table IF NOT EXISTS queues
(id SERIAL PRIMARY KEY,
 name TEXT UNIQUE NOT NULL);

Create TABLE IF NOT EXISTS messages
(id SERIAL PRIMARY KEY,
 queue_id INT NOT NULL,
 transaction_id INT,
 transaction_action SMALLINT NOT NULL,
 state SMALLINT NOT NULL,
 add_datetime TIMESTAMPTZ NOT NULL,
 close_datetime TIMESTAMPTZ,
 priority SMALLINT NOT NULL,
 max_attempts INT NOT NULL,
 attempts INT NOT NULL,
 expiry_datetime TIMESTAMPTZ NULL,  -- Null means it won't expire.
 correlation_id INT,
 group_name TEXT,
 metadata JSONB,
 payload BYTEA,
 FOREIGN KEY(queue_id) REFERENCES queues(id),
 FOREIGN KEY(transaction_id) REFERENCES transactions(id));

Create table IF NOT EXISTS tags
(id SERIAL PRIMARY KEY,
 tag_name TEXT,
 tag_value TEXT);

CREATE UNIQUE INDEX idx_tag_name ON tags(tag_name);

Create table IF NOT EXISTS message_tags
(tag_id INTEGER,
 message_id INTEGER,
 PRIMARY KEY(tag_id, message_id),
 FOREIGN KEY(tag_id) REFERENCES tags(id) ON DELETE CASCADE,
 FOREIGN KEY(message_id) REFERENCES messages(id) ON DELETE CASCADE);

GRANT ALL PRIVILEGES ON DATABASE nworkqueue TO nworkqueue_user;
GRANT USAGE ON SCHEMA public to nworkqueue_user;
--Not needed, yet
GRANT ALL ON ALL SEQUENCES IN SCHEMA public TO nworkqueue_user;
GRANT ALL ON ALL TABLES IN SCHEMA public TO nworkqueue_user;

--sample one.  Need to fill in with code to pull next and perform trans update.
CREATE OR REPLACE PROCEDURE procedure_name()
LANGUAGE plpgsql
AS $$
  DECLARE
    messageId int;
  begin
    START TRANSACTION;
      --SELECT * FROM messages FOR UPDATE SKIP LOCKED LIMIT 1;
    SELECT id into messageId, queue_id, transaction_id, transaction_action, state as MessageState, add_datetime, close_datetime, 
    priority, max_attempts, attempts, expiry_datetime, correlation_id, group_name, metadata, payload 
    FROM messages WHERE state = @MessageState AND close_datetime IS NULL AND transaction_id IS NULL AND 
    queue_id = @QueueId 
    ORDER BY priority DESC, add_datetime 
    FOR UPDATE SKIP LOCKED LIMIT 1; 

    Update messages set state = @NewMessageState, transaction_id = @TransactionId, transaction_action = @TransactionAction 
    WHERE id = messageId;

    COMMIT;
  end
$$;
