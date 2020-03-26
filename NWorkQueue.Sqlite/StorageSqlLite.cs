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
using NWorkQueue.Server.Common.Models;
using NWorkQueue.Server.Common;

namespace NWorkQueue.Sqlite
{
    /// <summary>
    /// Implements the IStorage interface for storing and retriving queue date to SQLite.
    /// </summary>
    public class StorageSqlLite : IStorage
    {
        //private SqliteConnection connection;
        private string connectionString;

        // IStorage methods below
        public StorageSqlLite(string connectionString)
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
                        State = (int)TransactionState.Active,
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

        public async ValueTask CommitTransaction(long transId)
        {
            throw new NotImplementedException();
            /*
            var storageTransaction = this.storage.BeginStorageTransaction();

            // Check if transaction has expired
            var transModel = this.storage.GetTransactionById(this.Id, storageTransaction);
            if (transModel == null)
            {
                return TransactionResult.NotFound;
            }

            if (!transModel.Active)
            {
                return TransactionResult.Closed;
            }

            if (transModel.ExpiryDateTime <= DateTime.Now)
            {
                // Took too long to run transaction, so now we have to rollback :-(
                this.RollbackTransaction(this.Id);
                return TransactionResult.Expired;
            }

            var commitDateTime = DateTime.Now;

            // Updated newly added messages
            this.storage.CommitAddedMessages(this.Id, storageTransaction);

            // Update newly completed messages
            this.storage.CommitPulledMessages(this.Id, storageTransaction, commitDateTime);

            // Update Transaction record
            this.storage.CommitMessageTransaction(this.Id, storageTransaction, commitDateTime);

            storageTransaction.Commit();
            return TransactionResult.Success;
            */
        }

        public ValueTask RollBackTransaction(long transId)
        {
            throw new NotImplementedException();
            /*
var storageTrans = this.storage.BeginStorageTransaction();
var closeDateTime = DateTime.Now;

// Close the transaction
this.storage.CloseTransaction(transId, storageTrans, closeDateTime);

// Removed messages added during the transaction
this.storage.DeleteMessagesByTransId(transId, storageTrans);

// Check if open messages are at the retry threshold, if so , mark as closed
this.storage.CloseRetriedMessages(transId, storageTrans, closeDateTime);

// Check if open messages are past the expiry date, if so mark as such
this.storage.ExpireOlderMessages(transId, storageTrans, closeDateTime, closeDateTime);

// All other records, increment retry count, mark record as active and ready to be pulled again
this.storage.UpdateRetriesOnRollbackedMessages(transId, storageTrans);

storageTrans.Commit();
*/

        }
        /*

        /// <inheritdoc/>
        public IStorageTransaction BeginStorageTransaction()
        {
            return new DbTransaction(this.connection);
        }

        /// <inheritdoc/>
        public void CloseTransaction(long transId, IStorageTransaction storageTrans, DateTime closeDateTime)
        {
            const string sql = "Update Transactions SET EndDateTime = @EndDateTime where Id = @tranId";
            this.connection.Execute(sql, new { transId, EndDateTime = closeDateTime }, (storageTrans as DbTransaction)?.SqliteTransaction);
        }

        /// <inheritdoc/>
        public void CommitMessageTransaction(long transId, IStorageTransaction storageTrans, DateTime commitDateTime)
        {
            const string sql = "UPDATE Transactions SET Active = 0, EndDateTime = @EndDateTime WHERE Id = @TranId;";
            this.connection.Execute(sql, transaction: (storageTrans as DbTransaction)?.SqliteTransaction, param: new { TranId = transId, EndDateTime = commitDateTime });
        }

        #endregion

        #region Queue methods
        
        /// <inheritdoc/>
        public long GetMaxQueueId()
        {
            const string sql = "SELECT Max(ID) FROM Queues;";
            var id = this.connection.ExecuteScalar<long?>(sql);
            if (id.HasValue)
            {
                return id.Value;
            }

            return 0;
        }
        */

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
        public async ValueTask DeleteQueue(long id/*, IStorageTransaction storageTrans*/)
        {
            await this.Execute(async (connection) =>
            {
                const string sql = "DELETE FROM Queues WHERE Id = @Id;";
                await connection.ExecuteAsync(sql,/* transaction: (storageTrans as DbTransaction)?.SqliteTransaction,*/ param: new { Id = id });
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
        public long GetMaxMessageId()
        {
            const string sql = "SELECT Max(ID) FROM Messages;";
            var id = this.connection.ExecuteScalar<int?>(sql);
            if (id.HasValue)
            {
                return id.Value + 1;
            }

            return 0;
        }

        /// <inheritdoc/>
        public void DeleteMessagesByTransId(long transId, IStorageTransaction storageTrans)
        {
            const string sql = "Delete FROM Messages WHERE TransactionId = @tranId and TransactionAction = @TranAction";
            this.connection.Execute(sql, new { transId, TranAction = TransactionAction.Add.Value }, (storageTrans as DbTransaction)?.SqliteTransaction);
        }

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
        }

        /// <inheritdoc/>
        public void CommitAddedMessages(long transId, IStorageTransaction storageTrans)
        {
            const string sql =
                "Update Messages SET State = @State, TransactionId = NULL, TransactionAction = NULL where TransactionId = @TranId  and TransactionAction = @TranAction;";
            this.connection.Execute(sql, transaction: (storageTrans as DbTransaction)?.SqliteTransaction, param: new { State = MessageState.Active.Value, TranId = transId, TranAction = TransactionAction.Add.Value });
        }

        /// <inheritdoc/>
        public void CommitPulledMessages(long transId, IStorageTransaction storageTrans, DateTime commitDateTime)
        {
            const string sql =
                "Update Messages SET State = @State, TransactionId = NULL, TransactionAction = NULL, CloseDateTime = @CloseDateTime where TransactionId = @TranId  and TransactionAction = @TranAction;";

            var param = new
            {
                State = MessageState.Processed.Value,
                TranId = transId,
                TranAction = TransactionAction.Pull.Value,
                CloseDateTime = commitDateTime
            };

            this.connection.Execute(
                sql,
                transaction: (storageTrans as DbTransaction)?.SqliteTransaction,
                param: param);
        }

        /// <inheritdoc/>
        public long GetMessageCount(long queueId)
        {
            const string sql = "SELECT count(*) FROM Messages m WHERE m.QueueId = @QueueId AND m.State = @State;";
            return this.connection.ExecuteScalar<long>(sql, new { QueueId = queueId, State = MessageState.Active.Value });
        }

        /// <inheritdoc/>
        public void AddMessage(long transId, IStorageTransaction? storageTrans, long nextId, long queueId, byte[] compressedMessage, DateTime addDateTime, string metaData = "", int priority = 0, int maxRetries = 3, DateTime? expiryDateTime = null, int correlation = 0, string groupName = "")
        {
            const string sql = "INSERT INTO Messages (Id, QueueId, TransactionId, TransactionAction, State, AddDateTime, Priority, MaxRetries, Retries, ExpiryDate, Data, CorrelationId, GroupName, Metadata) VALUES " +
                      "(@Id, @QueueId, @TransactionId, @TransactionAction, @State, @AddDateTime, @Priority, @MaxRetries, 0, @ExpiryDate, @Data, @CorrelationId, @GroupName, @Metadata);";
            var param = new
            {
                Id = nextId,
                QueueId = queueId,
                TransactionId = transId,
                TransactionAction = TransactionAction.Add.Value,
                State = MessageState.InTransaction.Value,
                AddDateTime = addDateTime,
                Priority = priority,
                MaxRetries = maxRetries,
                ExpiryDate = expiryDateTime ?? DateTime.MaxValue,
                Data = compressedMessage,
                CorrelationId = correlation,
                GroupName = groupName,
                Metadata = metaData
            };

            this.connection.Execute(sql, transaction: (storageTrans as DbTransaction)?.SqliteTransaction, param: param);
        }

        /// <inheritdoc/>
        void IStorage.DeleteMessagesByQueueId(long queueId, IStorageTransaction storageTrans)
        {
            const string sql = "Delete FROM Messages WHERE QueueId = @queueId;";
            this.connection.Execute(sql, new { queueId }, (storageTrans as DbTransaction)?.SqliteTransaction);
        }

        #endregion
        */

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
                " EndDateTime DATETIME);" +

                "Create table IF NOT EXISTS Queues" +
                "(Id INTEGER PRIMARY KEY," +
                " Name TEXT UNIQUE NOT NULL);" +

                "Create TABLE IF NOT EXISTS Messages " +
                "(Id INTEGER PRIMARY KEY," +
                " QueueId INTEGER NOT NULL, " +
                " TransactionId INTEGER," +
                " TransactionAction INTEGER," +
                " State INTEGER NOT NULL, " +
                " AddDateTime DATETIME NOT NULL," +
                " CloseDateTime DATETIME, " +
                " Priority INTEGER NOT NULL, " +
                " MaxRetries INTEGER NOT NULL, " +
                " Retries INTEGER NOT NULL, " +
                " ExpiryDateTime DateTime NOT NULL, " +
                " CorrelationId INTEGER, " +
                " GroupName TEXT, " +
                " Metadata TEXT, " +
                " Data BLOB," +
                " FOREIGN KEY(QueueId) REFERENCES Queues(Id), " +
                " FOREIGN KEY(TransactionId) REFERENCES Transactions(Id));";

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

        /// <summary>
        /// Executes an anonymous method wrapped with robust logging, command line options loading, and error handling
        /// </summary>
        /// <typeparam name="TOptions">Options class to load from command line</typeparam>
        /// <param name="programName">name of the executing program</param>
        /// <param name="args">Command line arguments</param>
        /// <param name="action">The anonymous method to execute. Contains a logging object and the options object, both of which can be accessed in the anonymous method.
        /// The method must be async.</param>
        /// <returns>returns an async Task</returns>
        public async ValueTask<T> Execute<T>(Func<SqliteConnection, ValueTask<T>> action, SqliteConnection? connection = null)
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
                //logger.Fatal(exc, "A fatal exception prevented this application from running to completion.");
            }
            finally
            {
                // If connection wasn't passed in, we must dispose of the resource manually
                if (connection == null)
                {
                    liveConnection?.Dispose();
                }
                //Log.CloseAndFlush();
            }
        }

        public async ValueTask Execute(Func<SqliteConnection, ValueTask> action, SqliteConnection? connection = null)
        {
            await this.Execute<object?>(
                async (newConnection) =>
                {
                    await action(newConnection);
                    return null;
                }, connection);
        }
        //Environment.Exit(returnStatus);
    }
}