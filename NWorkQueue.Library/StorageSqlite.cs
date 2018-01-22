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

        void IStorage.InitializeStorage(bool deleteExistingData, string settings)
        {
            //_con = new SqliteConnection(@"Data Source=:memory:;"); //About 30% faster, and NO durability
            _con = new SqliteConnection();
            _con.Open();
            if (deleteExistingData)
                DeleteAllTables();
            CreateTables(); //Non-destructive
        }

        long IStorage.GetMaxTransId()
        {
            var sql = "SELECT Max(ID) FROM TRANSACTIONS;";
            var id = _con.ExecuteScalar<int?>(sql);
            if (id.HasValue)
                return id.Value + 1;
            return 0;
        }

        long IStorage.GetMaxMessageId()
        {
            var sql = "SELECT Max(ID) FROM Messages;";
            var id = _con.ExecuteScalar<int?>(sql);
            if (id.HasValue)
                return id.Value + 1;
            return 0;
        }

        long IStorage.GetMaxQueueId()
        {
            var sql = "SELECT Max(ID) FROM Queues;";
            var id = _con.ExecuteScalar<long?>(sql);
            if (id.HasValue)
                return id.Value;
            return 0;
        }


        void IDisposable.Dispose()
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
                " Name TEXT NOT NULL);" +

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

        void IStorage.StartTransaction(long newId, int expiryTimeInMinutes)
        {
            var sql = "INSERT INTO Transactions (Id, Active, StartDateTime, ExpiryDateTime) VALUES (@Id, 1, @StartDateTime, @ExpiryDateTime)";
            _con.Execute(sql, new { StartDateTime = DateTime.Now, ExpiryDateTime = DateTime.Now.AddMinutes(expiryTimeInMinutes), Id = newId });
        }

        TransactionModel IStorage.GetTransactionById(long transId, IStorageTransaction storageTrans = null)
        {
            var sql = "SELECT * FROM Transactions WHERE Id = @Id";
            return _con.QueryFirstOrDefault<TransactionModel>(sql, new { Id = transId }, (storageTrans as InternalTransaction)?.SqliteTransaction);
        }

        void IStorage.UpdateTransaction(long transId, int expiryTimeInMinutes)
        {
            var sql = "UPDATE Transaction SET ExpiryDateTime = @DateTime WHERE Id = @Id";
            _con.Execute(sql, new { DateTime = DateTime.Now.AddMinutes(expiryTimeInMinutes), Id = transId });
        }

        IStorageTransaction IStorage.BeginStorageTransaction()
        {
            return new InternalTransaction() {SqliteTransaction = _con.BeginTransaction()};
        }

        void IStorage.CloseTransaction(long transId, IStorageTransaction storageTrans, DateTime closeDateTime)
        {
            var sql = "Update Transactions SET EndDateTime = @EndDateTime where Id = @tranId";
            _con.Execute(sql, new { transId, EndDateTime = closeDateTime }, (storageTrans as InternalTransaction)?.SqliteTransaction);
        }

        void IStorage.DeleteNewMessagesByTransId(long transId, IStorageTransaction storageTrans)
        {
            var sql = "Delete FROM Messages WHERE TransactionId = @tranId and TransactionAction = @TranAction";
            _con.Execute(sql, new { transId, TranAction = TransactionAction.Add.Value }, (storageTrans as InternalTransaction)?.SqliteTransaction);
        }

        void IStorage.CloseRetriedMessages(long transId, IStorageTransaction storageTrans)
        {
            var sql = "UPDATE Messages SET State = @State, TransactionId = NULL, TransactionAction = NULL, CloseDateTime = @CloseDateTime WHERE TransactionId = @tranId and TransactionAction = @TranAction and Retries >= MaxRetries";
            _con.Execute(sql, new { State = MessageState.RetryExceeded, transId, TranAction = TransactionAction.Pull.Value }, (storageTrans as InternalTransaction)?.SqliteTransaction);
        }

        void IStorage.ExpireOlderMessages(long transId, IStorageTransaction storageTrans, DateTime closeDateTime)
        {
            var sql = "UPDATE Messages SET State = @State, TransactionId = NULL, TransactionAction = NULL, CloseDateTime = @CloseDateTime WHERE TransactionId = @tranId and TransactionAction = @TranAction and ExpiryDate <= @ExpiryDate";
            _con.Execute(sql, new { State = MessageState.Expired, transId, TranAction = TransactionAction.Pull.Value, ExpiryDate = closeDateTime }, (storageTrans as InternalTransaction)?.SqliteTransaction);
        }

        void IStorage.UpdateRetriesOnRollbackedMessages(long transId, IStorageTransaction storageTrans)
        {
            var sql = "UPDATE Messages SET State = @State, TransactionId = NULL, TransactionAction = NULL, Retries = Retries + 1 WHERE TransactionId = @tranId and TransactionAction = @TranAction;";
            _con.Execute(sql, new { State = MessageState.Active, transId, TranAction = TransactionAction.Pull.Value }, (storageTrans as InternalTransaction)?.SqliteTransaction);
        }

        void IStorage.CommitAddedMessages(int transId, IStorageTransaction storageTrans)
        {
            var sql =
                "Update Messages SET STATE = @State AND TransactionId = NULL AND TransactionAction = NULL where TransactionId = @TranId  and TransactionAction = @TranAction;";
            _con.Execute(sql, transaction: (storageTrans as InternalTransaction)?.SqliteTransaction, param: new { State = MessageState.Active.Value, TranId = transId, TranAction = TransactionAction.Add.Value });
        }

        void IStorage.CommitPulledMessages(int transId, IStorageTransaction storageTrans, DateTime commitDateTime)
        {
            var sql =
                "Update Messages SET STATE = @State AND TransactionId = NULL AND TransactionAction = NULL AND CloseDateTime = @CloseDateTime where TransactionId = @TranId  and TransactionAction = @TranAction;";
            _con.Execute(sql, transaction: (storageTrans as InternalTransaction)?.SqliteTransaction,
                param: new
                {
                    State = MessageState.Processed.Value,
                    TranId = transId,
                    TranAction = TransactionAction.Pull.Value,
                    CloseDateTime = commitDateTime
                });
        }

        void IStorage.CommitMessageTransaction(int transId, IStorageTransaction storageTrans, DateTime commitDateTime)
        {
            var sql = "UPDATE Transactions SET Active = 0, EndDateTime = @EndDateTime WHERE Id = @TranId;";
            _con.Execute(sql, transaction: (storageTrans as InternalTransaction)?.SqliteTransaction, param: new { TranId = transId, EndDateTime = commitDateTime });
        }

        SortedList<string, WorkQueueModel> IStorage.GetFullQueueList()
        {
            var sql = "SELECT Name, Id FROM Queues;";
            return new SortedList<string, WorkQueueModel>(_con.Query(sql).ToDictionary(row => (string) row.Name,
                row => new WorkQueueModel() {Id = row.Id, Name = row.Name}));
        }

        void IStorage.AddQueue(long nextId, string name)
        {
            var sql = "INSERT INTO Queues (Id, Name) VALUES (@Id, @Name);";
            _con.Execute(sql, new { Id = nextId, Name = name });
        }

        void IStorage.DeleteQueue(long id, IStorageTransaction storageTrans)
        {
            var sql = "DELETE FROM Queues WHERE Id = @Id;";
            _con.Execute(sql, transaction: (storageTrans as InternalTransaction)?.SqliteTransaction, param: new { Id = id });

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
