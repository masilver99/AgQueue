using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dapper;
using Microsoft.Data.Sqlite;
using SQLitePCL;
using NWorkQueue.Common;
using NWorkQueue.Common.Models;

namespace NWorkQueue.Sqlite
{

    public class StorageSqlite : IStorage
    {
        private SqliteConnection _con;

        // IStorage methods below

        void IStorage.InitializeStorage(bool deleteExistingData, string settings)
        {
            // _con = new SqliteConnection(@"Data Source=:memory:;"); //About 30% faster, and NO durability
            this._con = new SqliteConnection(settings);
            this._con.Open();
            if (deleteExistingData)
            {
                this.DeleteAllTables();
            }

            this.CreateTables(); // Non-destructive
        }

        #region Transaction methods
        long IStorage.GetMaxTransId()
        {
            const string sql = "SELECT Max(ID) FROM TRANSACTIONS;";
            var id = this._con.ExecuteScalar<int?>(sql);
            if (id.HasValue)
            {
                return id.Value + 1;
            }

            return 0;
        }

        void IStorage.StartTransaction(long newId, int expiryTimeInMinutes)
        {
            const string sql = "INSERT INTO Transactions (Id, Active, StartDateTime, ExpiryDateTime) VALUES (@Id, 1, @StartDateTime, @ExpiryDateTime)";
            this._con.Execute(sql, new { StartDateTime = DateTime.Now, ExpiryDateTime = DateTime.Now.AddMinutes(expiryTimeInMinutes), Id = newId });
        }

        public TransactionModel GetTransactionById(long transId, IStorageTransaction storageTrans)
        {
            const string sql = "SELECT * FROM Transactions WHERE Id = @Id";
            return this._con.QueryFirstOrDefault<TransactionModel>(sql, new { Id = transId }, (storageTrans as InternalTransaction)?.SqliteTransaction);
        }

        public TransactionModel GetTransactionById(long transId)
        {
            return this.GetTransactionById(transId, null);
        }

        void IStorage.UpdateTransaction(long transId, int expiryTimeInMinutes)
        {
            const string sql = "UPDATE Transaction SET ExpiryDateTime = @DateTime WHERE Id = @Id";
            this._con.Execute(sql, new { DateTime = DateTime.Now.AddMinutes(expiryTimeInMinutes), Id = transId });
        }

        IStorageTransaction IStorage.BeginStorageTransaction()
        {
            return new InternalTransaction() { SqliteTransaction = _con.BeginTransaction() };
        }

        void IStorage.CloseTransaction(long transId, IStorageTransaction storageTrans, DateTime closeDateTime)
        {
            const string sql = "Update Transactions SET EndDateTime = @EndDateTime where Id = @tranId";
            this._con.Execute(sql, new { transId, EndDateTime = closeDateTime }, (storageTrans as InternalTransaction)?.SqliteTransaction);
        }

        void IStorage.CommitMessageTransaction(long transId, IStorageTransaction storageTrans, DateTime commitDateTime)
        {
            const string sql = "UPDATE Transactions SET Active = 0, EndDateTime = @EndDateTime WHERE Id = @TranId;";
            this._con.Execute(sql, transaction: (storageTrans as InternalTransaction)?.SqliteTransaction, param: new { TranId = transId, EndDateTime = commitDateTime });
        }

        #endregion

        #region Queue methods
        long IStorage.GetMaxQueueId()
        {
            const string sql = "SELECT Max(ID) FROM Queues;";
            var id = this._con.ExecuteScalar<long?>(sql);
            if (id.HasValue)
            {
                return id.Value;
            }

            return 0;
        }

        void IStorage.AddQueue(long nextId, string name)
        {
            const string sql = "INSERT INTO Queues (Id, Name) VALUES (@Id, @Name);";
            this._con.Execute(sql, new { Id = nextId, Name = name });
        }

        void IStorage.DeleteQueue(long id, IStorageTransaction storageTrans)
        {
            const string sql = "DELETE FROM Queues WHERE Id = @Id;";
            this._con.Execute(sql, transaction: (storageTrans as InternalTransaction)?.SqliteTransaction, param: new { Id = id });
        }

        // This search should be case sensitive, only use LIKE with SQLite
        long? IStorage.GetQueueId(string name)
        {
            const string sql = "SELECT ID FROM Queues WHERE Name LIKE @Name;";
            var id = this._con.ExecuteScalar<long?>(sql, new { Name = name });
            return id;
        }

        bool IStorage.DoesQueueExist(long id)
        {
            const string sql = "SELECT ID FROM Queues WHERE ID = @id;";
            var newId = this._con.ExecuteScalar<long?>(sql, new {id});
            return newId.HasValue;
        }
        #endregion

        #region Message methods
        long IStorage.GetMaxMessageId()
        {
            const string sql = "SELECT Max(ID) FROM Messages;";
            var id = this._con.ExecuteScalar<int?>(sql);
            if (id.HasValue)
            {
                return id.Value + 1;
            }

            return 0;
        }

        void IStorage.DeleteNewMessagesByTransId(long transId, IStorageTransaction storageTrans)
        {
            const string sql = "Delete FROM Messages WHERE TransactionId = @tranId and TransactionAction = @TranAction";
            this._con.Execute(sql, new { transId, TranAction = TransactionAction.Add.Value }, (storageTrans as InternalTransaction)?.SqliteTransaction);
        }

        void IStorage.CloseRetriedMessages(long transId, IStorageTransaction storageTrans)
        {
            const string sql = "UPDATE Messages SET State = @State, TransactionId = NULL, TransactionAction = NULL, CloseDateTime = @CloseDateTime WHERE TransactionId = @tranId and TransactionAction = @TranAction and Retries >= MaxRetries";
            this._con.Execute(sql, new { State = MessageState.RetryExceeded, transId, TranAction = TransactionAction.Pull.Value }, (storageTrans as InternalTransaction)?.SqliteTransaction);
        }

        void IStorage.ExpireOlderMessages(long transId, IStorageTransaction storageTrans, DateTime closeDateTime)
        {
            const string sql = "UPDATE Messages SET State = @State, TransactionId = NULL, TransactionAction = NULL, CloseDateTime = @CloseDateTime WHERE TransactionId = @tranId and TransactionAction = @TranAction and ExpiryDate <= @ExpiryDate";
            this._con.Execute(sql, new { State = MessageState.Expired, transId, TranAction = TransactionAction.Pull.Value, ExpiryDate = closeDateTime }, (storageTrans as InternalTransaction)?.SqliteTransaction);
        }

        void IStorage.UpdateRetriesOnRollbackedMessages(long transId, IStorageTransaction storageTrans)
        {
            const string sql = "UPDATE Messages SET State = @State, TransactionId = NULL, TransactionAction = NULL, Retries = Retries + 1 WHERE TransactionId = @tranId and TransactionAction = @TranAction;";
            this._con.Execute(sql, new { State = MessageState.Active, transId, TranAction = TransactionAction.Pull.Value }, (storageTrans as InternalTransaction)?.SqliteTransaction);
        }

        void IStorage.CommitAddedMessages(long transId, IStorageTransaction storageTrans)
        {
            const string sql =
                "Update Messages SET State = @State, TransactionId = NULL, TransactionAction = NULL where TransactionId = @TranId  and TransactionAction = @TranAction;";
            this._con.Execute(sql, transaction: (storageTrans as InternalTransaction)?.SqliteTransaction, param: new { State = MessageState.Active.Value, TranId = transId, TranAction = TransactionAction.Add.Value });
        }

        void IStorage.CommitPulledMessages(long transId, IStorageTransaction storageTrans, DateTime commitDateTime)
        {
            const string sql =
                "Update Messages SET State = @State, TransactionId = NULL, TransactionAction = NULL, CloseDateTime = @CloseDateTime where TransactionId = @TranId  and TransactionAction = @TranAction;";
            this._con.Execute(sql, transaction: (storageTrans as InternalTransaction)?.SqliteTransaction,
                param: new
                {
                    State = MessageState.Processed.Value,
                    TranId = transId,
                    TranAction = TransactionAction.Pull.Value,
                    CloseDateTime = commitDateTime
                });
        }

        public long GetMessageCount(long queueId)
        {
            const string sql = "SELECT count(*) FROM Messages m WHERE m.QueueId = @QueueId AND m.State = @State;";
            return this._con.ExecuteScalar<long>(sql, new { QueueId = queueId, State = MessageState.Active.Value });
        }

        public void AddMessage(long transId, IStorageTransaction storageTrans, long nextId, long queueId, byte[] compressedMessage, DateTime addDateTime, string metaData = "", int priority = 0, int maxRetries = 3, DateTime? expiryDateTime = null, int correlation = 0, string groupName = "")
        {
            const string sql = "INSERT INTO Messages (Id, QueueId, TransactionId, TransactionAction, State, AddDateTime, Priority, MaxRetries, Retries, ExpiryDate, Data, CorrelationId, GroupName, Metadata) VALUES " +
                      "(@Id, @QueueId, @TransactionId, @TransactionAction, @State, @AddDateTime, @Priority, @MaxRetries, 0, @ExpiryDate, @Data, @CorrelationId, @GroupName, @Metadata);";

            this._con.Execute(sql, transaction: (storageTrans as InternalTransaction)?.SqliteTransaction,
                param: new
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
                });
        }

        void IStorage.DeleteMessagesByQueueId(long queueId, IStorageTransaction storageTrans)
        {
            const string sql = "Delete FROM Messages WHERE QueueId = @queueId;";
            this._con.Execute(sql, new { queueId }, (storageTrans as InternalTransaction)?.SqliteTransaction);
        }

        #endregion

        void IDisposable.Dispose()
        {
            this._con.Dispose();
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
                "PRAGMA SYNCHRONOUS = NORMAL;" + //
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
                " ExpiryDate DateTime NOT NULL, " +
                " CorrelationId INTEGER, " +
                " GroupName TEXT, " +
                " Metadata TEXT, " +
                " Data BLOB," +
                " FOREIGN KEY(QueueId) REFERENCES Queues(Id), " +
                " FOREIGN KEY(TransactionId) REFERENCES Transactions(Id));";

            this._con.Execute(sql);
#pragma warning restore SA1515        }
        }

        private void DeleteAllTables()
        {
            const string sql =
                "BEGIN;" +
                "DROP TABLE IF EXISTS Messages; " +
                "DROP TABLE IF EXISTS Queues;" +
                "DROP table IF EXISTS Transactions;" +
                "COMMIT;";
            this._con.Execute(sql);
        }
    }

    internal class InternalTransaction : IStorageTransaction
    {
        internal SqliteTransaction SqliteTransaction { get; set; }

        void IStorageTransaction.Commit()
        {
            SqliteTransaction.Commit();
        }

        void IStorageTransaction.Rollback()
        {
            SqliteTransaction.Rollback();
        }
    }
}
