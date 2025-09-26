// <copyright file="StorageSqlite.cs" company="Michael Silver">
// Copyright (c) Michael Silver. All rights reserved.
// </copyright>

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;
using Dapper;
using AgQueue.Common;
using AgQueue.Common.Extensions;
using AgQueue.Common.Models;
using AgQueue.Server.Common;
using AgQueue.Server.Common.Models;
using System.Data;

namespace AgQueue.Postgres
{
    /// <summary>
    /// Implements the IStorage interface for storing and retrieving queue date to SQLite.
    /// </summary>
    public class StoragePostgres : IStorage
    {
        private readonly string connectionString;

        /// <summary>
        /// Initializes a new instance of the <see cref="StoragePostgre"/> class.
        /// </summary>
        /// <param name="connectionString">The Sqlite connection string.</param>
        public StoragePostgres(string connectionString)
        {
            this.connectionString = connectionString;
        }

        /// <inheritdoc/>
        public async ValueTask InitializeStorage(bool deleteExistingData)
        {
            await this.ExecuteAsync(
                async (connection) =>
                {
                    if (deleteExistingData)
                    {
                        await this.DeleteAllTables(connection);
                    }

                    await this.CreateTables(connection); // Non-destructive
                }, new NpgsqlConnection(this.connectionString));
        }

        /// <inheritdoc/>
        public async ValueTask<long> StartTransaction(DateTime startDateTime, DateTime expiryDateTime)
        {
            return await this.ExecuteAsync<long>(async (connection) =>
                {
                    // State of 1 = Active
                    const string sql = "INSERT INTO transactions (state, start_datetime, expiry_datetime) VALUES (@State, @StartDateTime, @ExpiryDateTime);SELECT last_insert_rowid();";
                    return await connection.ExecuteScalarAsync<long>(sql, new
                    {
                        State = TransactionState.Active,
                        StartDateTime = startDateTime,
                        ExpiryDateTime = expiryDateTime,
                    });
                });
        }

        /// <inheritdoc/>
        public async ValueTask ExtendTransaction(long transId, DateTime expiryDateTime)
        {
            await this.ExecuteAsync(async (connection) =>
            {
                const string sql = "UPDATE transactions SET expiry_datetime = @ExpiryDateTime WHERE ID = @Id;";
                await connection.ExecuteAsync(sql, new
                {
                    Id = transId,
                    ExpiryDateTime = expiryDateTime,
                });
            });
        }

        /// <inheritdoc/>
        public async ValueTask<Transaction?> GetTransactionById(long transId)
        {
            const string sql = "SELECT id, state, start_datetime, expiry_datetime, end_datetime FROM transactions WHERE Id = @Id";
            return await this.ExecuteAsync<Transaction?>(async (connection) =>
            {
                return await connection.QuerySingleOrDefaultAsync<Transaction?>(sql, new { Id = transId });
            });
        }

        /// <inheritdoc/>
        public async ValueTask UpdateTransactionState(IStorageTransaction storageTrans, long transId, TransactionState state, string? endReason = null, DateTime? endDateTime = null)
        {
            await this.ExecuteAsync(
                async (connection) =>
                {
                    const string sql = "UPDATE transactions SET end_datetime = @EndDateTime, state = @State, end_reason = @EndReason WHERE id = @Id;";
                    await connection.ExecuteAsync(
                        sql,
                        new
                        {
                            Id = transId,
                            State = state,
                            EndDateTime = endDateTime,
                            EndReason = endReason
                        },
                        storageTrans.NpgsqlTransaction());
                },
                storageTrans.NpgsqlTransaction().Connection);
        }

        /// <inheritdoc/>
        public async ValueTask<long> AddQueue(string name)
        {
            return await this.ExecuteAsync<long>(async (connection) =>
            {
                const string sql = "INSERT INTO queues (name) VALUES (@Name) returning id;";
                return await connection.ExecuteScalarAsync<long>(sql, new { Name = name });
            });
        }

        /// <inheritdoc/>
        public async ValueTask DeleteQueue(long id)
        {
            await this.ExecuteAsync(async (connection) =>
            {
                const string sql = "DELETE FROM queues WHERE id = @Id;";
                await connection.ExecuteAsync(sql, param: new { Id = id });
            });
        }

        /// <inheritdoc/>
        public async ValueTask<QueueInfo?> GetQueueInfoByName(string name)
        {
            return await this.ExecuteAsync<QueueInfo?>(async (connection) =>
            {
                const string sql = "SELECT id, name FROM queues WHERE name = @Name;";
                return await connection.QueryFirstOrDefaultAsync<QueueInfo?>(sql, new { Name = name });
            });
        }

        /// <inheritdoc/>
        public async ValueTask<QueueInfo?> GetQueueInfoById(long queueId)
        {
            return await this.ExecuteAsync<QueueInfo?>(async (connection) =>
            {
                const string sql = "SELECT id, name FROM queues WHERE id = @Id;";
                return await connection.QuerySingleOrDefaultAsync<QueueInfo?>(sql, new { Id = queueId });
            });
        }

        /// <inheritdoc/>
        public async ValueTask<long> AddMessage(
            long transId,
            long queueId,
            byte[] payload,
            DateTime addDateTime,
            string metaData,
            int priority,
            int maxAttempts,
            DateTime? expiryDateTime,
            int correlation,
            string groupName,
            TransactionAction transactionAction,
            MessageState messageState)
        {
            const string sql = "INSERT INTO messages (queue_id, transaction_id, transaction_action, state, add_datetime, priority, max_attempts, attempts, expiry_datetime, payload, correlation_id, group_name, metadata) VALUES " +
                      "(@QueueId, @TransactionId, @TransactionAction, @State, @AddDateTime, @Priority, @MaxAttempts, 0, @ExpiryDateTime, @Payload, @CorrelationId, @GroupName, @Metadata) returning id;";

            return await this.ExecuteAsync<long>(async (connection) =>
            {
                var param = new
                {
                    QueueId = queueId,
                    TransactionId = transId,
                    TransactionAction = transactionAction,
                    State = messageState,
                    AddDateTime = addDateTime,
                    Priority = priority,
                    MaxAttempts = maxAttempts,
                    ExpiryDateTime = expiryDateTime,
                    Payload = payload,
                    CorrelationId = correlation,
                    GroupName = groupName,
                    Metadata = metaData
                };

                return await connection.ExecuteScalarAsync<long>(sql, param: param);
            });
        }

        /// <inheritdoc/>
        public IStorageTransaction BeginStorageTransaction()
        {
            var connection = new NpgsqlConnection(this.connectionString);
            connection.Open();
            return new DbTransaction(connection);
        }

        /// <inheritdoc/>
        public async ValueTask<int> UpdateMessages(IStorageTransaction storageTrans, long transId, TransactionAction transactionAction, MessageState oldMessageState, MessageState newMessageState, DateTime? closeDateTime)
        {
            const string sql = "Update messages set transaction_id = null, transaction_action = 0,  " +
                    "state = @NewMessageState, close_datetime = @CloseDateTime " +
                    "where transaction_id = @TransId AND transaction_action = @TransactionAction AND state = @OldMessageState;";
            var sqliteConnection = storageTrans.NpgsqlTransaction().Connection;
            return await this.ExecuteAsync<int>(
                async (connection) =>
                {
                    return await connection.ExecuteAsync(
                        sql,
                        transaction: (storageTrans as DbTransaction)?.NpgsqlTransaction,
                        param: new
                        {
                            TransId = transId,
                            TransactionAction = transactionAction,
                            OldMessageState = oldMessageState,
                            NewMessageState = newMessageState,
                            CloseDateTime = closeDateTime
                        });
                },
                sqliteConnection);
        }

        /// <inheritdoc/>
        public async ValueTask<int> UpdateMessageAttemptCount(IStorageTransaction storageTrans, long transId, TransactionAction transactionAction, MessageState messageState)
        {
            const string sql = "Update messages set attempts = attempts + 1 " +
                    "where transaction_id = @TransId AND transaction_action = @TransactionAction AND state = @MessageState;";
            var sqliteConnection = storageTrans.NpgsqlTransaction().Connection;
            return await this.ExecuteAsync<int>(
                async (connection) =>
                {
                    return await connection.ExecuteAsync(
                        sql,
                        transaction: (storageTrans as DbTransaction)?.NpgsqlTransaction,
                        param: new
                        {
                            TransId = transId,
                            TransactionAction = transactionAction,
                            MessageState = messageState
                        });
                },
                sqliteConnection);
        }

        /// <inheritdoc/>
        public async ValueTask<int> DeleteAddedMessages(IStorageTransaction storageTrans, long transId)
        {
            const string sql = "DELETE FROM messages where transaction_id = @TransId AND transaction_action = @TransactionAction AND state = @MessageState;";
            var sqliteConnection = storageTrans.NpgsqlTransaction().Connection;
            return await this.ExecuteAsync<int>(
                async (connection) =>
                {
                    return await connection.ExecuteAsync(
                        sql,
                        transaction: (storageTrans as DbTransaction)?.NpgsqlTransaction,
                        param: new
                        {
                            TransId = transId,
                            TransactionAction = TransactionAction.Add,
                            MessageState = MessageState.InTransaction
                        });
                },
                sqliteConnection);
        }

        /// <inheritdoc/>
        public async ValueTask<int> DeleteAddedMessagesInExpiredTrans(IStorageTransaction storageTrans, DateTime currentDateTime)
        {
            const string sql =
                "Delete from messages where transaction_action = @TransactionAction AND state = @MessageState AND " +
                "transaction_id in (select id from transactions Where expiry_datetime <= @CurrentDateTime);";
            var sqliteConnection = storageTrans.NpgsqlTransaction().Connection;
            return await this.ExecuteAsync<int>(
                async (connection) =>
                {
                    return await connection.ExecuteAsync(
                        sql,
                        transaction: (storageTrans as DbTransaction)?.NpgsqlTransaction,
                        param: new
                        {
                            TransactionAction = TransactionAction.Add,
                            MessageState = MessageState.InTransaction,
                            CurrentDateTime = currentDateTime
                        });
                },
                sqliteConnection);
        }

        /// <inheritdoc/>
        public async ValueTask<int> UpdateMessageAttemptsInExpiredTrans(IStorageTransaction storageTrans, DateTime currentDateTime)
        {
            const string sql =
                "Update messages set attempts = attempts + 1, transaction_action = 0, transaction_id = null, state = @NewMessageState " +
                "where transaction_action = @TransactionAction AND state = @MessageState AND " +
                "transaction_id in (select id from transactions Where expiry_datetime <= @CurrentDateTime);";
            var sqliteConnection = storageTrans.NpgsqlTransaction().Connection;
            return await this.ExecuteAsync<int>(
                async (connection) =>
                {
                    return await connection.ExecuteAsync(
                        sql,
                        transaction: (storageTrans as DbTransaction)?.NpgsqlTransaction,
                        param: new
                        {
                            TransactionAction = TransactionAction.Pull,
                            MessageState = MessageState.InTransaction,
                            CurrentDateTime = currentDateTime,
                            NewMessageState = MessageState.Active // Return to active for now. We won't worry about closing it here, if it needs to be.
                        });
                },
                sqliteConnection);
        }

        /// <inheritdoc/>
        public async ValueTask<int> ExpireTransactions(IStorageTransaction storageTrans, DateTime currentDateTime)
        {
            const string sql =
                "Update transactions SET state = @NewTransactionState, end_datetime = @CurrentDateTime, end_reason = 'Expired' " +
                "Where expiry_datetime <= @CurrentDateTime AND state = @OldTransactionState;";
            var sqliteConnection = storageTrans.NpgsqlTransaction().Connection;
            return await this.ExecuteAsync<int>(
                async (connection) =>
                {
                    return await connection.ExecuteAsync(
                        sql,
                        transaction: (storageTrans as DbTransaction)?.NpgsqlTransaction,
                        param: new
                        {
                            NewTransactionState = TransactionState.Expired,
                            MessageState = MessageState.InTransaction,
                            CurrentDateTime = currentDateTime,
                            OldTransactionState = TransactionState.Active
                        });
                },
                sqliteConnection);
        }

        /// <inheritdoc/>
        public async ValueTask<int> ExpireMessages(DateTime currentDateTime)
        {
            const string sql =
                "Update messages set state = @NewMessageState, close_datetime = @CurrentDateTime where transaction_id IS null " +
                "and state = @OldMessageState and expiry_datetime <= @CurrentDateTime";

            return await this.ExecuteAsync<int>(async (connection) =>
            {
                return await connection.ExecuteAsync(
                    sql,
                    param: new
                    {
                        OldMessageState = MessageState.Active,
                        NewMessageState = MessageState.Expired,
                        CurrentDateTime = currentDateTime
                    });
            });
        }

        /// <inheritdoc/>
        public async ValueTask<int> CloseMaxAttemptsExceededMessages(DateTime currentDateTime)
        {
            const string sql =
                "Update messages set state = @NewMessageState, close_datetime = @CurrentDateTime where transaction_id IS null and state = @OldMessageState and attempts >= max_attempts;";

            return await this.ExecuteAsync<int>(async (connection) =>
            {
                return await connection.ExecuteAsync(
                    sql,
                    param: new
                    {
                        NewMessageState = MessageState.AttemptsExceeded,
                        OldMessageState = MessageState.Active,
                        CurrentDateTime = currentDateTime
                    });
            });
        }

        /// <inheritdoc/>
        public async ValueTask<Message?> DequeueMessage(
            long transId,
            long queueId)
        {
            // USe select for no key updates here or select for share
            // Add skip locked to end of select.  It will avoid delays
            // http://shiroyasha.io/selecting-for-share-and-update-in-postgresql.html
            // This routine is the bread and butter of the queue.  It must ensure only the next record is returned.
            return await this.ExecuteAsync<Message?>(async (connection) =>
            {
                var sql = "dequeue_message";
                var message = await connection.QuerySingleOrDefaultAsync<Message?>(
                    sql,
                    new { p_queue_id = queueId, p_transaction_id = transId },
                    commandType: CommandType.StoredProcedure);
                return message;
            });
        }

        /// <inheritdoc/>
        public async ValueTask<Message?> PeekMessageByQueueId(long queueId)
        {
            return await this.ExecuteAsync<Message?>(async (connection) =>
            {
                return await this.GetNextMessage(connection, queueId);
            });
        }

        /// <inheritdoc/>
        public async ValueTask<Message?> PeekMessageByMessageId(long messageId)
        {
            return await this.ExecuteAsync<Message?>(async (connection) =>
            {
                const string sql =
                "SELECT id, queue_id, transaction_id, transaction_action, state as MessageState, add_datetime, close_datetime, " +
                "priority, max_attempts, attempts, expiry_datetime, correlation_id, group_name, metadata, payload " +
                "FROM messages WHERE id = @MessageId;";

                return await connection.QuerySingleOrDefaultAsync<Message?>(
                    sql,
                    param: new
                    {
                        MessageId = messageId
                    });
            });
        }

        private async ValueTask<Message?> GetNextMessage(NpgsqlConnection connection, long queueId)
        {
            const string sql =
                "SELECT id, queue_id, transaction_id, transaction_action, state as MessageState, add_datetime, close_datetime, " +
                "priority, max_attempts, attempts, expiry_datetime, correlation_id, group_name, metadata, payload " +
                "FROM messages WHERE state = @MessageState AND close_datetime IS NULL AND transaction_id IS NULL AND " +
                "queue_id = @QueueId " +
                "ORDER BY priority DESC, add_datetime " +
                "LIMIT 1 ";
            return await connection.QuerySingleOrDefaultAsync<Message?>(
                sql,
                param: new
                {
                    MessageState = MessageState.Active,
                    QueueId = queueId
                });
        }

        private async ValueTask UpdateMessageWithTransaction(NpgsqlConnection connection, long messageId, long transId)
        {
            const string sql =
                "Update messages set state = @NewMessageState, transaction_id = @TransactionId, transaction_action = @TransactionAction " +
                "WHERE id = @MessageId;";
            await connection.ExecuteAsync(
                sql,
                param: new
                {
                    NewMessageState = MessageState.InTransaction,
                    TransactionId = transId,
                    MessageId = messageId,
                    TransactionAction = TransactionAction.Pull
                });
        }

        /// <summary>
        /// Executes an anonymous method wrapped with robust logging, command line options loading, and error handling.
        /// </summary>
        /// <typeparam name="T">Options class to load from command line.</typeparam>
        /// <param name="action">The anonymous method execute. Contains a logging object and the options object, both of which can be accessed in the anonymous method.</param>
        /// <param name="connection">Sqlite connection.</param>
        /// <returns>Returns generic object T.</returns>
        private async ValueTask<T> ExecuteAsync<T>(Func<NpgsqlConnection, ValueTask<T>> action, NpgsqlConnection? connection = null)
        {
            // using (var logger = Utilities.BuildLogger(programName, EnvironmentConfig.AspNetCoreEnvironment, EnvironmentConfig.SeqServerUrl))
            // logger.Information("Starting {ProgramName}", programName);
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            // AppDomain.CurrentDomain.UnhandledException += Utilities.CurrentDomain_UnhandledException;
            NpgsqlConnection? liveConnection = null;

            try
            {
                if (connection == null)
                {
                    liveConnection = new NpgsqlConnection(this.connectionString);
                }
                else
                {
                    liveConnection = connection;
                }

                T returnValue = await action(liveConnection);
                stopwatch.Stop();
                return returnValue;

                // logger.Information("{ProgramName} completed in {ElapsedTime}ms", programName, stopwatch.ElapsedMilliseconds);
            }
            catch (Exception exc)
            {
                throw exc;

                // logger.Fatal(exc, "A fatal exception prevented this application from running to completion.");
            }
            finally
            {
                // If connection wasn't passed in, we must dispose of the resource manually
                if (connection == null)
                {
                    liveConnection?.Close();
                    liveConnection?.Dispose();
                }

                // Log.CloseAndFlush();
            }
        }

        /// <summary>
        /// Executes an anonymous method wrapped with robust logging, command line options loading, and error handling.
        /// </summary>
        /// <typeparam name="T">Options class to load from command line.</typeparam>
        /// <param name="action">The anonymous method execute. Contains a logging object and the options object, both of which can be accessed in the anonymous method.</param>
        /// <param name="connection">Sqlite connection.</param>
        /// <returns>Returns generic object T.</returns>
        private T ExecuteAsync<T>(Func<NpgsqlConnection, T> action, NpgsqlConnection? connection = null)
        {
            // using (var logger = Utilities.BuildLogger(programName, EnvironmentConfig.AspNetCoreEnvironment, EnvironmentConfig.SeqServerUrl))
            // logger.Information("Starting {ProgramName}", programName);
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            // AppDomain.CurrentDomain.UnhandledException += Utilities.CurrentDomain_UnhandledException;
            NpgsqlConnection? liveConnection = null;

            try
            {
                if (connection == null)
                {
                    liveConnection = new NpgsqlConnection(this.connectionString);
                }
                else
                {
                    liveConnection = connection;
                }

                T returnValue = action(liveConnection);
                stopwatch.Stop();
                return returnValue;

                // logger.Information("{ProgramName} completed in {ElapsedTime}ms", programName, stopwatch.ElapsedMilliseconds);
            }
            catch (Exception exc)
            {
                throw exc;

                // logger.Fatal(exc, "A fatal exception prevented this application from running to completion.");
            }
            finally
            {
                // If connection wasn't passed in, we must dispose of the resource manually
                if (connection == null)
                {
                    liveConnection?.Close();
                    liveConnection?.Dispose();
                }

                // Log.CloseAndFlush();
            }
        }

        private async ValueTask CreateTables(NpgsqlConnection connection)
        {
#pragma warning disable SA1515

            const string sql =
                "Create table IF NOT EXISTS transactions" +
                "(id SERIAL PRIMARY KEY,," +
                " state SMALLINT NOT NULL," +
                " start_datetime timestamptz NOT NULL," +
                " expiry_datetimeDateTime timestamptz NOT NULL, " +
                " end_datetime timestamptz," +
                " end_reason TEXT);" +

                "Create table IF NOT EXISTS queues" +
                "(id SERIAL PRIMARY KEY," +
                " name TEXT UNIQUE NOT NULL);" +

                "Create TABLE IF NOT EXISTS messages " +
                "(id SERIAL PRIMARY KEY," +
                " queue_id INT NOT NULL, " +
                " transaction_id INT, " +
                " transaction_action SMALLINT NOT NULL, " +
                " state SMALLINT NOT NULL, " +
                " add_datetime TIMESTAMPTZ NOT NULL," +
                " close_datetime TIMESTAMPTZ, " +
                " priority SMALLINT NOT NULL, " +
                " max_attempts INT NOT NULL, " +
                " attempts INT NOT NULL, " +
                " expiry_datetime TIMESTAMPTZ NULL, " + // Null means it won't expire.
                " correlation_id INT, " +
                " group_name TEXT, " +
                " metadata JSONB, " +
                " payload BYTEA," +
                " FOREIGN KEY(queue_id) REFERENCES queues(id), " +
                " FOREIGN KEY(transaction_id) REFERENCES transactions(id));" +

                "Create table IF NOT EXISTS tags" +
                "(id SERIAL PRIMARY KEY," +
                " tag_name TEXT," +
                " tag_value TEXT);" +

                "CREATE UNIQUE INDEX idx_tag_name ON tags(tag_name); " +

                "Create table IF NOT EXISTS message_tags" +
                "(tag_id INTEGER, " +
                " message_id INTEGER, " +
                " PRIMARY KEY(tag_id, message_id), " +
                " FOREIGN KEY(tag_id) REFERENCES tags(id) ON DELETE CASCADE, " +
                " FOREIGN KEY(message_id) REFERENCES messages(id) ON DELETE CASCADE); ";
        /*
        "Create TABLE IF NOT EXISTS MessageTransactions " +
        "(TransactionId INTEGER, " +
        " MessageId INTEGER, " +
        " TransactionStatus INTEGER, " +
        " PRIMARY KEY(TransactionId, MessageId), " +
        " FOREIGN KEY(TransactionId) REFERENCES Transactions(Id), " +
        " FOREIGN KEY(MessageId) REFERENCES Messages(Id));";
        */
                await connection.ExecuteAsync(sql);
                #pragma warning restore SA1515
        }

        private async ValueTask DeleteAllTables(NpgsqlConnection connection)
        {
            const string sql =
                "BEGIN;" +
                "DROP TABLE IF EXISTS message_tags; " +
                "DROP TABLE IF EXISTS tags; " +
                "DROP TABLE IF EXISTS messages; " +
                "DROP TABLE IF EXISTS queues;" +
                "DROP table IF EXISTS transactions;" +
                "COMMIT;";
            await connection.ExecuteAsync(sql);
        }

        public void Dispose()
        {
            // Do nothing
        }
    }
}