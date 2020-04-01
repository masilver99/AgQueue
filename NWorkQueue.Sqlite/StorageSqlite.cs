// <copyright file="StorageSqlite.cs" company="Michael Silver">
// Copyright (c) Michael Silver. All rights reserved.
// </copyright>

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Data.Sqlite;
using NWorkQueue.Common;
using NWorkQueue.Common.Models;
using NWorkQueue.Server.Common;
using NWorkQueue.Server.Common.Models;

namespace NWorkQueue.Sqlite
{
    /// <summary>
    /// Implements the IStorage interface for storing and retriving queue date to SQLite.
    /// </summary>
    public class StorageSqlite : IStorage
    {
        private readonly string connectionString;

        /// <summary>
        /// Initializes a new instance of the <see cref="StorageSqlite"/> class.
        /// </summary>
        /// <param name="connectionString">The Sqlite connection string.</param>
        public StorageSqlite(string connectionString)
        {
            this.connectionString = connectionString;
        }

        /// <inheritdoc/>
        public async ValueTask InitializeStorage(bool deleteExistingData)
        {
            await this.Execute(
                async (connection) =>
                {
                    if (deleteExistingData)
                    {
                        await this.DeleteAllTables(connection);
                    }

                    await this.CreateTables(connection); // Non-destructive
                }, new SqliteConnection(this.connectionString));
        }

        /// <inheritdoc/>
        public async ValueTask<long> StartTransaction(DateTime startDateTime, DateTime expiryDateTime)
        {
            return await this.Execute<long>(async (connection) =>
                {
                    // State of 1 = Active
                    const string sql = "INSERT INTO Transactions (State, StartDateTime, ExpiryDateTime) VALUES (@State, @StartDateTime, @ExpiryDateTime);SELECT last_insert_rowid();";
                    return await connection.ExecuteScalarAsync<long>(sql, new
                    {
                        State = TransactionState.Active.Value,
                        StartDateTime = startDateTime,
                        ExpiryDateTime = expiryDateTime,
                    });
                });
        }

        /// <inheritdoc/>
        public async ValueTask ExtendTransaction(long transId, DateTime expiryDateTime)
        {
            await this.Execute(async (connection) =>
            {
                const string sql = "UPDATE Transactions SET ExpiryDateTime = @ExpiryDateTime WHERE ID = @Id;";
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
            const string sql = "SELECT Id, State, StartDateTime, ExpiryDateTime, EndDateTime FROM Transactions WHERE Id = @Id";
            return await this.Execute<Transaction?>(async (connection) =>
            {
                return await connection.QuerySingleOrDefaultAsync<Transaction?>(sql, new { Id = transId });
            });
        }

        /// <inheritdoc/>
        public async ValueTask UpdateTransactionState(IStorageTransaction storageTrans, long transId, TransactionState state, string? endReason = null, DateTime? endDateTime = null)
        {
            await this.Execute(async (connection) =>
            {
                const string sql = "UPDATE Transactions SET EndDateTime = @EndDateTime, State = @State, EndReason = @EndReason WHERE ID = @Id;";
                await connection.ExecuteAsync(
                    sql,
                    new
                    {
                        Id = transId,
                        State = state,
                        EndDateTime = endDateTime,
                        endReason
                    },
                    storageTrans.SqliteTransaction());
            });
        }

        /// <inheritdoc/>
        public async ValueTask<long> AddQueue(string name)
        {
            return await this.Execute<long>(async (connection) =>
            {
                const string sql = "INSERT INTO Queues (Name) VALUES (@Name);SELECT last_insert_rowid();";
                return await connection.ExecuteScalarAsync<long>(sql, new { Name = name });
            });
        }

        /// <inheritdoc/>
        public async ValueTask DeleteQueue(long id)
        {
            await this.Execute(async (connection) =>
            {
                const string sql = "DELETE FROM Queues WHERE Id = @Id;";
                await connection.ExecuteAsync(sql, param: new { Id = id });
            });
        }

        /// <inheritdoc/>
        public async ValueTask<QueueInfo?> GetQueueInfoByName(string name)
        {
            return await this.Execute<QueueInfo?>(async (connection) =>
            {
                const string sql = "SELECT ID, NAME FROM Queues WHERE Name = @Name;";
                return await connection.QueryFirstOrDefaultAsync<QueueInfo?>(sql, new { Name = name });
            });
        }

        /// <inheritdoc/>
        public async ValueTask<QueueInfo?> GetQueueInfoById(long queueId)
        {
            return await this.Execute<QueueInfo?>(async (connection) =>
            {
                const string sql = "SELECT ID, NAME FROM Queues WHERE ID = @Id;";
                return await connection.QuerySingleOrDefaultAsync<QueueInfo?>(sql, new { Id = queueId });
            });
        }

        /*
        #region Message methods

        /// <inheritdoc/>
        public void CloseRetriedMessages(long transId, IStorageTransaction storageTrans, DateTime closeDateTime)
        {
            const string sql = "UPDATE Messages SET State = @State, TransactionId = NULL, TransactionAction = NULL, CloseDateTime = @closeDateTime WHERE TransactionId = @tranId and TransactionAction = @TranAction and Retries >= MaxRetries";
            this.connection.Execute(sql, new { State = MessageState.RetryExceeded, transId, TranAction = TransactionAction.Pull.Value, closeDateTime }, (storageTrans as DbTransaction)?.SqliteTransaction);
        }

        /// <inheritdoc/>
        public void ExpireOlderMessages(long transId, IStorageTransaction storageTrans, DateTime closeDateTime, DateTime expiryDateTime)
        {
            const string sql = "UPDATE Messages SET State = @State, TransactionId = NULL, TransactionAction = NULL, CloseDateTime = @closeDateTime WHERE TransactionId = @tranId and TransactionAction = @TranAction and ExpiryDateTime <= @expiryDateTime";
            this.connection.Execute(sql, new { State = MessageState.Expired, transId, TranAction = TransactionAction.Pull.Value, expiryDateTime, closeDateTime }, (storageTrans as DbTransaction)?.SqliteTransaction);
        }

        /// <inheritdoc/>
        public void UpdateRetriesOnRollbackedMessages(long transId, IStorageTransaction storageTrans)
        {
            const string sql = "UPDATE Messages SET State = @State, TransactionId = NULL, TransactionAction = NULL, Retries = Retries + 1 WHERE TransactionId = @tranId and TransactionAction = @TranAction;";
            this.connection.Execute(sql, new { State = MessageState.Active, transId, TranAction = TransactionAction.Pull.Value }, (storageTrans as DbTransaction)?.SqliteTransaction);
            */

        /// <inheritdoc/>
        public async ValueTask<long> AddMessage(
            long transId,
            long queueId,
            byte[] payload,
            DateTime addDateTime,
            string metaData,
            int priority,
            int maxRetries,
            DateTime? expiryDateTime,
            int correlation,
            string groupName,
            int transactionAction,
            int messageState)
        {
            const string sql = "INSERT INTO Messages (QueueId, TransactionId, TransactionAction, State, AddDateTime, Priority, MaxRetries, Retries, ExpiryDate, Payload, CorrelationId, GroupName, Metadata) VALUES " +
                      "(@QueueId, @TransactionId, @TransactionAction, @State, @AddDateTime, @Priority, @MaxRetries, 0, @ExpiryDate, @Payload, @CorrelationId, @GroupName, @Metadata);" +
                      "SELECT last_insert_rowid();";

            return await this.Execute<long>(async (connection) =>
            {
                var param = new
                {
                    QueueId = queueId,
                    TransactionId = transId,
                    TransactionAction = transactionAction,
                    State = messageState,
                    AddDateTime = addDateTime,
                    Priority = priority,
                    MaxRetries = maxRetries,
                    ExpiryDate = expiryDateTime,
                    Payload = payload,
                    CorrelationId = correlation,
                    GroupName = groupName,
                    Metadata = metaData
                };

                return await connection.ExecuteScalarAsync<long>(sql, param: param);
            });
        }

        /*
        /// <inheritdoc/>
        void IStorage.DeleteMessagesByQueueId(long queueId, IStorageTransaction storageTrans)
        {
            const string sql = "Delete FROM Messages WHERE QueueId = @queueId;";
            this.connection.Execute(sql, new { queueId }, (storageTrans as DbTransaction)?.SqliteTransaction);
        }

        #endregion
        */

        /// <inheritdoc/>
        public IStorageTransaction BeginStorageTransaction()
        {
            var connection = new SqliteConnection(this.connectionString);
            return new DbTransaction(connection);
        }

        /// <inheritdoc/>
        public async ValueTask<int> UpdateMessages(IStorageTransaction storageTrans, long transId, int transactionAction, int oldMessageState, int newMessageState)
        {
            const string sql = "Update Messages set TransactionId = null, TransactionAction = null,  " +
                    "State = @NewMessageState, CloseDateTime DATETIME, " +
                    "where transactionId = @TransId AND TransactionAction = @TransactionAction AND State = @OldMessageState;";

            return await this.Execute<int>(async (connection) =>
            {
                return await connection.ExecuteAsync(
                    sql,
                    transaction: (storageTrans as DbTransaction)?.SqliteTransaction,
                    param: new { transId, transactionAction, oldMessageState, newMessageState });
            });
        }

        /// <inheritdoc/>
        public async ValueTask<int> UpdateMessageRetryCount(IStorageTransaction storageTrans, long transId, int transactionAction, int messageState)
        {
            const string sql = "Update Messages set Retries = Retries + 1 " +
                    "where transactionId = @TransId AND TransactionAction = @TransactionAction AND State = @MessageState;";

            return await this.Execute<int>(async (connection) =>
            {
                return await connection.ExecuteAsync(
                    sql,
                    transaction: (storageTrans as DbTransaction)?.SqliteTransaction,
                    param: new { transId, transactionAction, messageState });
            });
        }

        /// <inheritdoc/>
        public async ValueTask<int> DeleteAddedMessages(IStorageTransaction storageTrans, long transId)
        {
            const string sql = "DELETE FROM Messages where transactionId = @TransId AND TransactionAction = @TransactionAction AND State = @MessageState;";

            return await this.Execute<int>(async (connection) =>
            {
                return await connection.ExecuteAsync(
                    sql,
                    transaction: (storageTrans as DbTransaction)?.SqliteTransaction,
                    param: new { transId, TransactionAction = TransactionAction.Add.Value, MessageState = MessageState.InTransaction.Value });
            });
        }

        /// <inheritdoc/>
        public async ValueTask<int> DeleteAddedMessagesInExpiredTrans(IStorageTransaction storageTrans, DateTime currentDateTime)
        {
            const string sql =
                "Delete from messages where TransactionAction = @TransactionAction, State = @MessageState AND " +
                "TransactionId in (select ID from Transactions Where ExpiryDateTime <= @CurrentDateTime);";

            return await this.Execute<int>(async (connection) =>
            {
                return await connection.ExecuteAsync(
                    sql,
                    transaction: (storageTrans as DbTransaction)?.SqliteTransaction,
                    param: new
                    {
                        TransactionAction = TransactionAction.Add.Value,
                        MessageState = MessageState.InTransaction.Value,
                        currentDateTime
                    });
            });
        }

        /// <inheritdoc/>
        public async ValueTask<int> UpdateMessageRetriesInExpiredTrans(IStorageTransaction storageTrans, DateTime currentDateTime)
        {
            const string sql =
                "Update messages set Retries = Retries + 1, Set TransactionAction = null and TransactionId = null " +
                "where TransactionAction = @TransactionAction AND State = @MessageState AND " +
                "TransactionId in (select ID from Transactions Where ExpiryDateTime <= @CurrentDateTime);";

            return await this.Execute<int>(async (connection) =>
            {
                return await connection.ExecuteAsync(
                    sql,
                    transaction: (storageTrans as DbTransaction)?.SqliteTransaction,
                    param: new
                    {
                        TransactionAction = TransactionAction.Pull.Value,
                        MessageState = MessageState.InTransaction.Value,
                        currentDateTime
                    });
            });
        }

        /// <inheritdoc/>
        public async ValueTask<int> ExpireTransactions(IStorageTransaction storageTrans, DateTime currentDateTime)
        {
            const string sql =
                "Update Transactions SET State = @NewTransactionState, EndDateTime = @CurrentDateTime, EndReason = 'Expired' " +
                "Where ExpiryDateTime <= @CurrentDateTime AND State = @OldTransactionState;";

            return await this.Execute<int>(async (connection) =>
            {
                return await connection.ExecuteAsync(
                    sql,
                    transaction: (storageTrans as DbTransaction)?.SqliteTransaction,
                    param: new
                    {
                        NewTransactionState = TransactionState.Expired.Value,
                        MessageState = MessageState.InTransaction.Value,
                        currentDateTime,
                        OldTransactionState = TransactionState.Active.Value
                    });
            });
        }

        /// <summary>
        /// Executes an anonymous method wrapped with robust logging, command line options loading, and error handling.
        /// </summary>
        /// <typeparam name="T">Options class to load from command line.</typeparam>
        /// <param name="action">The anonymous method execute. Contains a logging object and the options object, both of which can be accessed in the anonymous method.</param>
        /// <param name="connection">Sqlite connection.</param>
        /// <returns>Returns generic object T.</returns>
        private async ValueTask<T> Execute<T>(Func<SqliteConnection, ValueTask<T>> action, SqliteConnection? connection = null)
        {
            // using (var logger = Utilities.BuildLogger(programName, EnvironmentConfig.AspNetCoreEnvironment, EnvironmentConfig.SeqServerUrl))
            // logger.Information("Starting {ProgramName}", programName);
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            // AppDomain.CurrentDomain.UnhandledException += Utilities.CurrentDomain_UnhandledException;
            SqliteConnection? liveConnection = null;

            try
            {
                if (connection == null)
                {
                    liveConnection = new SqliteConnection(this.connectionString);
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
                    liveConnection?.Dispose();
                }

                // Log.CloseAndFlush();
            }
        }

        private async ValueTask Execute(Func<SqliteConnection, ValueTask> action, SqliteConnection? connection = null)
        {
            await this.Execute<object?>(
                async (newConnection) =>
                {
                    await action(newConnection);
                    return null;
                }, connection);
        }

        private async ValueTask CreateTables(SqliteConnection connection)
        {
#pragma warning disable SA1515

            const string sql =
                "PRAGMA foreign_keys = ON;" +
                "PRAGMA TEMP_STORE = MEMORY;" +

                // Single-line comment must be preceded by blank line
                // "PRAGMA JOURNAL_MODE = PERSIST;" + //Slower than WAL by about 20+x
                // "PRAGMA SYNCHRONOUS = FULL;" +       //About 15x slower than NORMAL
                "PRAGMA SYNCHRONOUS = NORMAL;" +
                // Single-line comment must be preceded by blank line
                "PRAGMA LOCKING_MODE = EXCLUSIVE;" +
                "PRAGMA journal_mode = WAL;" +
                // "PRAGMA CACHE_SIZE = 500;" +
                "Create table IF NOT EXISTS Transactions" +
                "(Id INTEGER PRIMARY KEY," +
                " State INTEGER NOT NULL," +
                " StartDateTime DATETIME NOT NULL," +
                " ExpiryDateTime DATETIME NOT NULL, " +
                " EndDateTime DATETIME," +
                " EndReason TEXT);" +

                "Create table IF NOT EXISTS Queues" +
                "(Id INTEGER PRIMARY KEY," +
                " Name TEXT UNIQUE NOT NULL);" +

                "Create TABLE IF NOT EXISTS Messages " +
                "(Id INTEGER PRIMARY KEY," +
                " QueueId INTEGER NOT NULL, " +
                " TransactionId INTEGER, " +
                " TransactionAction INTEGER, " +
                " State INTEGER NOT NULL, " +
                " AddDateTime DATETIME NOT NULL," +
                " CloseDateTime DATETIME, " +
                " Priority INTEGER NOT NULL, " +
                " MaxRetries INTEGER NOT NULL, " +
                " Retries INTEGER NOT NULL, " +
                " ExpiryDateTime DateTime NULL, " + // Null means it won't expire.
                " CorrelationId INTEGER, " +
                " GroupName TEXT, " +
                " Metadata TEXT, " +
                " Payload BLOB," +
                " FOREIGN KEY(QueueId) REFERENCES Queues(Id), " +
                " FOREIGN KEY(TransactionId) REFERENCES Transactions(Id));";
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

        private async ValueTask DeleteAllTables(SqliteConnection connection)
        {
            const string sql =
                "BEGIN;" +
                "DROP TABLE IF EXISTS Messages; " +
                "DROP TABLE IF EXISTS Queues;" +
                "DROP table IF EXISTS Transactions;" +
                "COMMIT;";
            await connection.ExecuteAsync(sql);
        }
    }
}