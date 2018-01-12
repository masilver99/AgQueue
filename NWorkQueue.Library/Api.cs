using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using Dapper;
using Microsoft.Data.Sqlite;
using SQLitePCL;


namespace NWorkQueue.Library
{
    public class Api
    {
        internal SqliteConnection _con;

        private int _transId = 0;
        internal int _messageId = 0;
        private int _queueId = 0;

        private QueueList _queueList = new QueueList();

        static Regex _queueNameRegex = new Regex(@"^[A-Za-z0-9\.\-_]+$", RegexOptions.Compiled);

        //Settings
        //How long until a transcation expires and is automatically rolled back
        private int _expiryTimeInMinutes = 30;

        public Api(bool freshDatabase = false)
        {
            InitializeDb(freshDatabase);
        }

        private void InitializeDb(bool wipeDatabase)
        {
            _con = new SqliteConnection(@"Data Source=SqlLite.db;");
            _con.Open();
            if (wipeDatabase)
                DeleteAllTables();
            CreateTables();
            _transId = GetTransId();
            _messageId = GetMessageId();
            _queueId = GetQueueId();
            _queueList.Reload(LoadQueueList());
        }


        #region Transactions
        public Transaction StartTransaction()
        {
            var sql = "INSERT INTO Transactions (Id, Active, StartDateTime, ExpiryDateTime) VALUES (@Id, 1, @StartDateTime, @ExpiryDateTime)";
            var newId = Interlocked.Increment(ref _transId);
            _con.Execute(sql, new { StartDateTime = DateTime.Now, ExpiryDateTime = DateTime.Now.AddMinutes(_expiryTimeInMinutes), Id = newId });
            return new Transaction() {Id = newId, Api = this};
        }

        /// <summary>
        /// Updates the specified transaction, reseting it's timeout
        /// </summary>
        /// <param name="trans"></param>
        public void UpdateTransaction(Transaction trans)
        {
            var sql = "SELECT * FROM Transactions WHERE Id = @Id";
            var transModel = _con.QueryFirstOrDefault<TransactionModel>(sql, new {Id = trans.Id});
            if (transModel == null)
                throw new Exception("Unable to find transaction");
            if (transModel.Active != 1)
                throw new Exception("Cannot update closed transaction");

            sql = "UPDATE Transaction SET ExpiryDateTime = @DateTime WHERE Id = @Id";
            _con.Execute(sql, new {DateTime = DateTime.Now.AddMinutes(_expiryTimeInMinutes), Id = trans.Id});
        }

        internal void CommitTransaction(int tranId)
        {
            //Search all messages for trans
            //possible options in a trans Add or Pull
            //For adds, erase trans id from message
            //For pulls, mark message complete
            
        }

        internal void RollbackTransaction(int tranId)
        {
            
        }

        private int GetTransId()
        {
            var sql = "SELECT Max(ID) FROM TRANSACTIONS;";
            var id = _con.ExecuteScalar<int?>(sql);
            if (id.HasValue)
                return id.Value;
            return 0;
        }

        private int GetMessageId()
        {
            var sql = "SELECT Max(ID) FROM Messages;";
            var id = _con.ExecuteScalar<int?>(sql);
            if (id.HasValue)
                return id.Value;
            return 0;
        }

        #endregion

        private void CreateTables()
        {
            var sql = "PRAGMA foreign_keys = ON;" +
                      "PRAGMA TEMP_STORE = MEMORY;" +
                      //"PRAGMA JOURNAL_MODE = PERSIST;" +  //Don't readd, causes transaction failures
                      "PRAGMA SYNCHRONOUS = NORMAL;" +
                      //"PRAGMA LOCKING_MODE = EXCLUSIVE;"+  //Don't readd, causes transaction failures
                      //"PRAGMA journal_mode = MEMORY;"+   //Don't readd, causes transaction failures
                      "PRAGMA CACHE_SIZE = 500;" +

                      "BEGIN;" +
                      "Create table IF NOT EXISTS Transactions" +
                      "(Id INTEGER PRIMARY KEY," +
                      " Active INTEGER NOT NULL," +
                      " StartDateTime DATETIME NOT NULL," +
                      " ExpiryDateTime DATETIME NOT NULL);" +

                      "Create table IF NOT EXISTS Queues" +
                      "(Id INTEGER PRIMARY KEY," +
                      " Name TEXT NOT NULL);" +

                "Create TABLE IF NOT EXISTS Messages " + 
                "(Id INTEGER PRIMARY KEY," +
                " QueueId INTEGER NOT NULL, "+
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
                " FOREIGN KEY(QueueId) REFERENCES Queues(Id), "+
                " FOREIGN KEY(TransactionId) REFERENCES Transactions(Id));"+
                "COMMIT;";
                
            _con.Execute(sql);
        }

        private void DeleteAllTables()
        {
            var sql =
                "BEGIN;"+
                "DROP TABLE IF EXISTS Messages; " +
                "DROP TABLE IF EXISTS Queues;" +
                "DROP table IF EXISTS Transactions;"+
                "COMMIT;";
            _con.Execute(sql);
        }


        #region Queues
        private int GetQueueId()
        {
            var sql = "SELECT Max(ID) FROM Queues;";
            var id = _con.ExecuteScalar<int?>(sql);
            if (id.HasValue)
                return id.Value;
            return 0;
        }

        private void ValidateQueueName(in string queueName)
        {
            if (!_queueNameRegex.IsMatch(queueName))
                throw new ArgumentException("Queue name can only contain a-Z, 0-9, ., -, or _");
        }

        private SortedList<string, WorkQueueModel> LoadQueueList()
        {
            var sql = "SELECT Name, Id FROM Queues;";
            return new SortedList<string, WorkQueueModel>(_con.Query(sql).ToDictionary(row => (string) row.Name, row => new WorkQueueModel(){Id = row.Id, Name = row.Name }));
        }

        public WorkQueue CreateQueue(String name)
        {
            if (name.Length == 0)
                throw new ArgumentException("Queue name cannot be empty", nameof(name));
            ValidateQueueName(in name);
            if (_queueList.ContainsKey(name))
                    throw new Exception("Queue already exists");
            var trans = _con.BeginTransaction();
            var sql = "INSERT INTO Queues (Id, Name) VALUES (@Id, @Name);";
            var nextId = Interlocked.Increment(ref _queueId);
            _con.Execute(sql, transaction: trans, param: new {Id = nextId, Name = name });
            trans.Commit();
            var queueModel = new WorkQueueModel() {Id = nextId, Name = name};
            _queueList.Add(queueModel);
            return new WorkQueue(this, queueModel);
        }

        public WorkQueue GetQueue(string queueName)
        {
            if (queueName.Length == 0)
                throw new ArgumentException("Queue name cannot be empty", nameof(queueName));
            ValidateQueueName(in queueName);
            if (!_queueList.TryGetQueue(queueName, out WorkQueueModel workQueueModel))
                throw new Exception("Queue does not exist");
            return new WorkQueue(this, workQueueModel);
        }

        public void DeleteQueue(String name)
        {
            var fixedName = name.Trim();
            if (fixedName.Length == 0)
                throw new ArgumentException("Queue name cannot be empty", nameof(name));
            if (!_queueList.TryGetQueueId(fixedName, out var id))
                throw new Exception("Queue not found");
            var trans = _con.BeginTransaction();

            //TODO: Rollback queue transactions that were being used in message for this queue
            //TODO: Delete Messages

            //TODO: Delete actual queue table

            //Delete From Queue Table
            var sql = "DELETE FROM Queues WHERE Id = @Id;";
            _con.Execute(sql, transaction: trans, param: new { Id = id });
            trans.Commit();
            _queueList.Delete(fixedName);
        }

        #endregion
    }

    public class WorkQueue
    {
        internal Api Api { get; set; }
        internal WorkQueueModel QueueModel { get; set; }

        private SqliteCommand cmd { get;set; }
        internal WorkQueue(Api api, WorkQueueModel queueModel)
        {
            Api = api;
            QueueModel = queueModel;
            var sql = "BEGIN EXCLUSIVE;INSERT INTO Messages (Id, QueueId, TransactionId, TransactionAction, State, AddDateTime, Priority, MaxRetries, Retries, ExpiryDate, Data) VALUES " +
                      "(@Id, @QueueId, @TransactionId, @TransactionAction, @State, @AddDateTime, @Priority, @MaxRetries, 0, @ExpiryDate, @Data);COMMIT;";
            cmd = Api._con.CreateCommand();
            cmd.CommandText = sql;
            cmd.Prepare();

        }

        public void AddMessage(Transaction trans, Object message, int priority)
        {
            var nextId = Interlocked.Increment(ref Api._messageId);
            //var dbTrans = Api._con.BeginTransaction();
            //cmd.Transaction = dbTrans;
            cmd.Parameters.Clear();
            cmd.Parameters.AddWithValue("@Id", nextId);
            cmd.Parameters.AddWithValue("@QueueId", QueueModel.Id);
            cmd.Parameters.AddWithValue("@TransactionId", trans.Id);
            cmd.Parameters.AddWithValue("@TransactionAction", TransactionAction.Add.Value);
            cmd.Parameters.AddWithValue("@State", MessageState.InTransaction.Value);
            cmd.Parameters.AddWithValue("@AddDateTime", DateTime.Now);
            cmd.Parameters.AddWithValue("@Priority", 0);
            cmd.Parameters.AddWithValue("@MaxRetries", 3);
            cmd.Parameters.AddWithValue("@ExpiryDate", DateTime.MaxValue);
            cmd.Parameters.AddWithValue("@Data", MessagePack.LZ4MessagePackSerializer.Serialize(message));
            cmd.ExecuteNonQuery();
            //dbTrans.Commit();
        }

        public void AddMessage(Transaction trans, Object[] messages, int priority)
        {

            //Validate Transaction here
            var dbTrans = Api._con.BeginTransaction();
            var sql = "INSERT INTO Messages (Id, QueueId, TransactionId, TransactionAction, State, AddDateTime, Priority, MaxRetries, Retries, ExpiryDate, Data) VALUES " +
                      "(@Id, @QueueId, @TransactionId, @TransactionAction, @State, @AddDateTime, @Priority, @MaxRetries, @Retries, @ExpiryDate, @Data);";
            foreach (var message in messages)
            {
                var nextId = Interlocked.Increment(ref Api._messageId);
                Api._con.Execute(sql, transaction: dbTrans, param: new
                {
                    Id = nextId,
                    QueueId = QueueModel.Id,
                    TransactionId = trans.Id,
                    TransactionAction = TransactionAction.Add.Value,
                    State = MessageState.InTransaction.Value,
                    AddDateTime = DateTime.Now,
                    Priority = 0,
                    MaxRetries = 3,
                    Retries = 0,
                    ExpiryDate = DateTime.MaxValue,
                    Data = MessagePack.LZ4MessagePackSerializer.Serialize(message)
                });
            }

            dbTrans.Commit();
        }

        public void AddMessagePre(Transaction trans, Object[] messages, int priority)
        {
            //Validate Transaction here
            var dbTrans = Api._con.BeginTransaction();
            var sql = "INSERT INTO Messages (Id, QueueId, TransactionId, TransactionAction, State, AddDateTime, Priority, MaxRetries, Retries, ExpiryDate, Data) VALUES " +
                      "(@Id, @QueueId, @TransactionId, @TransactionAction, @State, @AddDateTime, @Priority, @MaxRetries, @Retries, @ExpiryDate, @Data);";
            var cmd = Api._con.CreateCommand();
            cmd.CommandText = sql;
            cmd.Prepare();
            cmd.Transaction = dbTrans;

            foreach (var message in messages)
            {
                var nextId = Interlocked.Increment(ref Api._messageId);
                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@Id", nextId);
                cmd.Parameters.AddWithValue("@QueueId", QueueModel.Id);
                cmd.Parameters.AddWithValue("@TransactionId", trans.Id);
                cmd.Parameters.AddWithValue("@TransactionAction", TransactionAction.Add.Value);
                cmd.Parameters.AddWithValue("@State", MessageState.InTransaction.Value);
                cmd.Parameters.AddWithValue("@AddDateTime",  DateTime.Now);
                cmd.Parameters.AddWithValue("@Priority",  0);
                cmd.Parameters.AddWithValue("@MaxRetries", 3);
                cmd.Parameters.AddWithValue("@Retries", 0);
                cmd.Parameters.AddWithValue("@ExpiryDate", DateTime.MaxValue);
                cmd.Parameters.AddWithValue("@Data", MessagePack.LZ4MessagePackSerializer.Serialize(message));
                cmd.ExecuteNonQuery();
            }

            dbTrans.Commit();
        }


    }

    internal class MessageWrapper
    {
    }

    public class Transaction
    {
        internal int Id { get; set; }
        internal Api Api;

        public void Commit()
        {
            Api.CommitTransaction(Id);
        }

        public void Rollback()
        {
            Api.RollbackTransaction(Id);
        }
    }

    internal class TransactionModel
    {
        public int Id { get; set; }
        public int Active { get; set; }
        public DateTime CreateDateTime { get; set; }
        public DateTime ExpiryDateTime { get; set; }
    }

    internal class MessageModel
    {
        /// <summary>
        /// Generated unique message id
        /// </summary>
        public int Id { get; set; }
        public int QueueId { get; set; }
        public int TransactionId { get; set; }
        public int TransactionAction { get; set; }
        public DateTime AddDateTime { get; set; }
        public DateTime CloseDateTime { get; set; }
        public int Priority { get; set; }

        /// <summary>
        /// Numerber of attempts to have message processed, i.e. commited
        /// </summary>
        public int MaxRetries { get; set; }
        /// <summary>
        /// Number of Rollbacks or timeouts before the message expires
        /// </summary>
        public int Retries { get; set; } = 0;
        /// <summary>
        /// DateTime the message will expire
        /// </summary>
        public DateTime ExpiryDate { get; set; }
        public int CorrelationId { get; set; }
        public string Group { get; set; }

        /// <summary>
        /// Actual message data 
        /// </summary>
        public byte[] Data { get; set; }

    }

    internal class WorkQueueModel
    {
        public Int64 Id { get; set; }
        public string Name { get; set; }
    }

    internal sealed class TransactionAction
    {
        public static readonly TransactionAction Add = new TransactionAction("Add", 0);
        public static readonly TransactionAction Pull = new TransactionAction("Pull", 1);

        public readonly string Name;
        public readonly int Value;

        private TransactionAction(string name, int value)
        {
            Name = name;
            Value = value;
        }

    }

    internal sealed class MessageState
    {
        public static readonly MessageState Active = new MessageState("Active", 0);
        public static readonly MessageState InTransaction = new MessageState("InTransaction", 1);
        public static readonly MessageState Processed = new MessageState("Processed", 2);
        public static readonly MessageState Expired = new MessageState("Expired", 3);
        public static readonly MessageState RetryExceeded = new MessageState("RetryExceeded", 4);

        public readonly string Name;
        public readonly int Value;

        private MessageState(string name, int value)
        {
            Name = name;
            Value = value;
        }
    }

    internal class QueueQueries
    {
        public CommandQuery AddMessage { get; set; } = new CommandQuery() {Sql = ""};
    }

    internal class CommandQuery
    {
        public String Sql { get; set; }
    }
}
