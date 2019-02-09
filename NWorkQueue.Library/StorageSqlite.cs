using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dapper;
using Microsoft.Data.Sqlite;
using SQLitePCL;

namespace NWorkQueue.Library
{
    internal class StorageSqlite : IStorage
    {
        private SqliteConnection _con;

        public void InitializeStorage(bool deleteExistingData, string settings)
        {
            //_con = new SqliteConnection(@"Data Source=:memory:;"); //About 30% faster, and NO durability
            _con = new SqliteConnection(settings);
            _con.Open();
            if (deleteExistingData)
                DeleteAllTables();
            CreateTables(); //Non-destructive
        }

        #region Transaction methods
        public long GetMaxTransId()
        {
            var sql = "SELECT Max(ID) FROM TRANSACTIONS;";
            var id = _con.ExecuteScalar<int?>(sql);
            if (id.HasValue)
                return id.Value + 1;
            return 0;
        }

        public void StartTransaction(long newId, int expiryTimeInMinutes)
        {
            var sql = "INSERT INTO Transactions (Id, Active, StartDateTime, ExpiryDateTime) VALUES (@Id, 1, @StartDateTime, @ExpiryDateTime)";
            _con.Execute(sql, new { StartDateTime = DateTime.Now, ExpiryDateTime = DateTime.Now.AddMinutes(expiryTimeInMinutes), Id = newId });
        }

        public TransactionModel GetTransactionById(long transId, IStorageTransaction storageTrans = null)
        {
            var sql = "SELECT * FROM Transactions WHERE Id = @Id";
            return _con.QueryFirstOrDefault<TransactionModel>(sql, new { Id = transId }, (storageTrans as InternalTransaction)?.SqliteTransaction);
        }

        public void UpdateTransaction(long transId, int expiryTimeInMinutes)
        {
            var sql = "UPDATE Transaction SET ExpiryDateTime = @DateTime WHERE Id = @Id";
            _con.Execute(sql, new { DateTime = DateTime.Now.AddMinutes(expiryTimeInMinutes), Id = transId });
        }

        public IStorageTransaction BeginStorageTransaction()
        {
            return new InternalTransaction() { SqliteTransaction = _con.BeginTransaction() };
        }

        public void CloseTransaction(long transId, IStorageTransaction storageTrans, DateTime closeDateTime)
        {
            var sql = "Update Transactions SET EndDateTime = @EndDateTime where Id = @tranId";
            _con.Execute(sql, new { transId, EndDateTime = closeDateTime }, (storageTrans as InternalTransaction)?.SqliteTransaction);
        }

        public void CommitMessageTransaction(long transId, IStorageTransaction storageTrans, DateTime commitDateTime)
        {
            var sql = "UPDATE Transactions SET Active = 0, EndDateTime = @EndDateTime WHERE Id = @TranId;";
            _con.Execute(sql, transaction: (storageTrans as InternalTransaction)?.SqliteTransaction, param: new { TranId = transId, EndDateTime = commitDateTime });
        }

        #endregion

        #region Queue methods
        public long GetMaxQueueId()
        {
            var sql = "SELECT Max(ID) FROM Queues;";
            var id = _con.ExecuteScalar<long?>(sql);
            if (id.HasValue)
                return id.Value;
            return 0;
        }

        public SortedList<string, WorkQueueModel> GetFullQueueList()
        {
            var sql = "SELECT Name, Id FROM Queues;";
            return new SortedList<string, WorkQueueModel>(_con.Query(sql).ToDictionary(row => (string)row.Name,
                row => new WorkQueueModel() { Id = row.Id, Name = row.Name }));
        }

        public void AddQueue(long nextId, string name)
        {
            var sql = "INSERT INTO Queues (Id, Name) VALUES (@Id, @Name);";
            _con.Execute(sql, new { Id = nextId, Name = name });
        }

        public void DeleteQueue(long id, IStorageTransaction storageTrans)
        {
            var sql = "DELETE FROM Queues WHERE Id = @Id;";
            _con.Execute(sql, transaction: (storageTrans as InternalTransaction)?.SqliteTransaction, param: new { Id = id });
        }

        // This search should be case sensitive, only use LIKE with SQLite
        public long? GetQueueId(string name)
        {
            var sql = "SELECT ID FROM Queues WHERE Name LIKE @Name;";
            var id = _con.ExecuteScalar<long?>(sql, new {Name = name});
            return id;
        }

        public bool DoesQueueExist(long id)
        {
            var sql = "SELECT ID FROM Queues WHERE ID = @id;";
            var newId = _con.ExecuteScalar<long?>(sql, new {id});
            return newId.HasValue;
        }
        #endregion

        #region Message methods
        public long GetMaxMessageId()
        {
            var sql = "SELECT Max(ID) FROM Messages;";
            var id = _con.ExecuteScalar<int?>(sql);
            if (id.HasValue)
                return id.Value + 1;
            return 0;
        }

        public void DeleteNewMessagesByTransId(long transId, IStorageTransaction storageTrans)
        {
            var sql = "Delete FROM Messages WHERE TransactionId = @tranId and TransactionAction = @TranAction";
            _con.Execute(sql, new { transId, TranAction = TransactionAction.Add.Value }, (storageTrans as InternalTransaction)?.SqliteTransaction);
        }

        public void CloseRetriedMessages(long transId, IStorageTransaction storageTrans)
        {
            var sql = "UPDATE Messages SET State = @State, TransactionId = NULL, TransactionAction = NULL, CloseDateTime = @CloseDateTime WHERE TransactionId = @tranId and TransactionAction = @TranAction and Retries >= MaxRetries";
            _con.Execute(sql, new { State = MessageState.RetryExceeded, transId, TranAction = TransactionAction.Pull.Value }, (storageTrans as InternalTransaction)?.SqliteTransaction);
        }

        public void ExpireOlderMessages(long transId, IStorageTransaction storageTrans, DateTime closeDateTime)
        {
            var sql = "UPDATE Messages SET State = @State, TransactionId = NULL, TransactionAction = NULL, CloseDateTime = @CloseDateTime WHERE TransactionId = @tranId and TransactionAction = @TranAction and ExpiryDate <= @ExpiryDate";
            _con.Execute(sql, new { State = MessageState.Expired, transId, TranAction = TransactionAction.Pull.Value, ExpiryDate = closeDateTime }, (storageTrans as InternalTransaction)?.SqliteTransaction);
        }

        public void UpdateRetriesOnRollbackedMessages(long transId, IStorageTransaction storageTrans)
        {
            var sql = "UPDATE Messages SET State = @State, TransactionId = NULL, TransactionAction = NULL, Retries = Retries + 1 WHERE TransactionId = @tranId and TransactionAction = @TranAction;";
            _con.Execute(sql, new { State = MessageState.Active, transId, TranAction = TransactionAction.Pull.Value }, (storageTrans as InternalTransaction)?.SqliteTransaction);
        }

        public void CommitAddedMessages(long transId, IStorageTransaction storageTrans)
        {
            var sql =
                "Update Messages SET State = @State, TransactionId = NULL, TransactionAction = NULL where TransactionId = @TranId  and TransactionAction = @TranAction;";
            _con.Execute(sql, transaction: (storageTrans as InternalTransaction)?.SqliteTransaction, param: new { State = MessageState.Active.Value, TranId = transId, TranAction = TransactionAction.Add.Value });
        }

        public void CommitPulledMessages(long transId, IStorageTransaction storageTrans, DateTime commitDateTime)
        {
            var sql =
                "Update Messages SET State = @State, TransactionId = NULL, TransactionAction = NULL, CloseDateTime = @CloseDateTime where TransactionId = @TranId  and TransactionAction = @TranAction;";
            _con.Execute(sql, transaction: (storageTrans as InternalTransaction)?.SqliteTransaction,
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
            var sql = "SELECT count(*) FROM Messages m WHERE m.QueueId = @QueueId AND m.State = @State;";
            return _con.ExecuteScalar<long>(sql, new { QueueId = queueId, State = MessageState.Active.Value });
        }

        public void AddMessage(long transId, IStorageTransaction storageTrans, long nextId, long queueId, byte[] compressedMessage, DateTime addDateTime, string metaData = "", int priority = 0, int maxRetries = 3, DateTime? expiryDateTime = null, int correlation = 0, string groupName = "")
        {
            var sql = "INSERT INTO Messages (Id, QueueId, TransactionId, TransactionAction, State, AddDateTime, Priority, MaxRetries, Retries, ExpiryDate, Data, CorrelationId, GroupName, Metadata) VALUES " +
                      "(@Id, @QueueId, @TransactionId, @TransactionAction, @State, @AddDateTime, @Priority, @MaxRetries, 0, @ExpiryDate, @Data, @CorrelationId, @GroupName, @Metadata);";

            _con.Execute(sql, transaction: (storageTrans as InternalTransaction)?.SqliteTransaction, param: new
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

        public void DeleteMessagesByQueueId(long queueId, IStorageTransaction storageTrans)
        {
            var sql = "Delete FROM Messages WHERE QueueId = @queueId;";
            _con.Execute(sql, new { queueId }, (storageTrans as InternalTransaction)?.SqliteTransaction);
        }

        #endregion

        public void Dispose()
        {
            _con.Dispose();
        }

        private void CreateTables()
        {
            var sql =
                "PRAGMA foreign_keys = ON;" +
                "PRAGMA TEMP_STORE = MEMORY;" +
                //"PRAGMA JOURNAL_MODE = PERSIST;" + //Slower than WAL by about 20+x
                //"PRAGMA SYNCHRONOUS = FULL;" +       //About 15x slower than NORMAL
                "PRAGMA SYNCHRONOUS = NORMAL;" +   //
                "PRAGMA LOCKING_MODE = EXCLUSIVE;" +
                "PRAGMA journal_mode = WAL;" +
                //"PRAGMA CACHE_SIZE = 500;" +

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

            _con.Execute(sql);
        }

        private void DeleteAllTables()
        {
            var sql =
                "BEGIN;" +
                "DROP TABLE IF EXISTS Messages; " +
                "DROP TABLE IF EXISTS Queues;" +
                "DROP table IF EXISTS Transactions;" +
                "COMMIT;";
            _con.Execute(sql);
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
