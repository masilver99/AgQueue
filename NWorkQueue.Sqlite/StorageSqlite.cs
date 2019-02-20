// <copyright file="StorageSqlite.cs" company="Michael Silver">
// Copyright (c) Michael Silver. All rights reserved.
// </copyright>

namespace NWorkQueue.Sqlite
{
    using System;
    using Dapper;
    using Microsoft.Data.Sqlite;
    using NWorkQueue.Common;
    using NWorkQueue.Common.Models;

    /// <summary>
    /// Implements the IStorage interface for storing and retriving queue date to SQLite
    /// </summary>
    public class StorageSqlite : IStorage
    {
        private SqliteConnection connection;

        // IStorage methods below

        /// <inheritdoc/>
        public void InitializeStorage(bool deleteExistingData, string settings)
        {
            // connection = new SqliteConnection(@"Data Source=:memory:;"); //About 30% faster, and NO durability
            this.connection = new SqliteConnection(settings);
            this.connection.Open();
            if (deleteExistingData)
            {
                this.DeleteAllTables();
            }

            this.CreateTables(); // Non-destructive
        }

        #region Transaction methods

        /// <inheritdoc/>
        public long GetMaxTransactionId()
        {
            const string sql = "SELECT Max(ID) FROM TRANSACTIONS;";
            var id = this.connection.ExecuteScalar<int?>(sql);
            if (id.HasValue)
            {
                return id.Value + 1;
            }

            return 0;
        }

        /// <inheritdoc/>
        public void StartTransaction(long newId, int expiryTimeInMinutes)
        {
            const string sql = "INSERT INTO Transactions (Id, Active, StartDateTime, ExpiryDateTime) VALUES (@Id, 1, @StartDateTime, @ExpiryDateTime)";
            this.connection.Execute(sql, new { StartDateTime = DateTime.Now, ExpiryDateTime = DateTime.Now.AddMinutes(expiryTimeInMinutes), Id = newId });
        }

        /// <inheritdoc/>
        public TransactionModel GetTransactionById(long transId, IStorageTransaction storageTrans = null)
        {
            const string sql = "SELECT * FROM Transactions WHERE Id = @Id";
            return this.connection.QueryFirstOrDefault<TransactionModel>(sql, new { Id = transId }, (storageTrans as DbTransaction)?.SqliteTransaction);
        }

        /// <inheritdoc/>
        public void ExtendTransaction(long transId, int expiryTimeInMinutes)
        {
            const string sql = "UPDATE Transaction SET ExpiryDateTime = @DateTime WHERE Id = @Id";
            this.connection.Execute(sql, new { DateTime = DateTime.Now.AddMinutes(expiryTimeInMinutes), Id = transId });
        }

        /// <inheritdoc/>
        public IStorageTransaction BeginStorageTransaction()
        {
            return new DbTransaction() { SqliteTransaction = this.connection.BeginTransaction() };
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

        /// <inheritdoc/>
        public void AddQueue(long nextId, string name)
        {
            const string sql = "INSERT INTO Queues (Id, Name) VALUES (@Id, @Name);";
            this.connection.Execute(sql, new { Id = nextId, Name = name });
        }

        /// <inheritdoc/>
        public void DeleteQueue(long id, IStorageTransaction storageTrans)
        {
            const string sql = "DELETE FROM Queues WHERE Id = @Id;";
            this.connection.Execute(sql, transaction: (storageTrans as DbTransaction)?.SqliteTransaction, param: new { Id = id });
        }

        /// <inheritdoc/>
        public long? GetQueueId(string name)
        {
            const string sql = "SELECT ID FROM Queues WHERE Name LIKE @Name;";
            var id = this.connection.ExecuteScalar<long?>(sql, new { Name = name });
            return id;
        }

        /// <inheritdoc/>
        public bool DoesQueueExist(long id)
        {
            const string sql = "SELECT ID FROM Queues WHERE ID = @id;";
            var newId = this.connection.ExecuteScalar<long?>(sql, new { id });
            return newId.HasValue;
        }
        #endregion

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
        public void AddMessage(long transId, IStorageTransaction storageTrans, long nextId, long queueId, byte[] compressedMessage, DateTime addDateTime, string metaData = "", int priority = 0, int maxRetries = 3, DateTime? expiryDateTime = null, int correlation = 0, string groupName = "")
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

        /// <inheritdoc/>
        void IDisposable.Dispose()
        {
            this.connection.Dispose();
        }

        private void CreateTables()
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
                " Active INTEGER NOT NULL," +
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

            this.connection.Execute(sql);
#pragma warning restore SA1515
        }

        private void DeleteAllTables()
        {
            const string sql =
                "BEGIN;" +
                "DROP TABLE IF EXISTS Messages; " +
                "DROP TABLE IF EXISTS Queues;" +
                "DROP table IF EXISTS Transactions;" +
                "COMMIT;";
            this.connection.Execute(sql);
        }
    }
}
