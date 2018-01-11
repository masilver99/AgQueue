using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using Dapper;
using LiteDB;
using Microsoft.Data.Sqlite;


namespace NWorkQueue.Library
{
    public class Api
    {
        private SqliteConnection _con;

        private int _transId = 0;
        private int _messageId = 0;
        private int _queueId = 0;

        private QueueList _queueList = new QueueList();

        static Regex _queueNameRegex = new Regex(@"[A-Z,a-z,0-9,\.,\-,_]+", RegexOptions.Compiled);

        //Settings
        //How long until a transcation expires and is automatically rolled back
        private int _expiryTimeInMinutes = 30;

        public Api()
        {
            InitializeDb();
        }

        private void InitializeDb()
        {
            _con = new SqliteConnection(@"Data Source=SqlLite.db;");
            _con.Open();
            //check tables exist
            //If not call CreateTable
            CreateTables();
            _transId = GetTransId();
            //_messageId = GetMessageId();
            _queueId = GetQueueId();
            _queueList.Reload(LoadQueueList());
        }

        #region Transactions
        public Transaction StartTransaction()
        {
            var sql = "INSERT INTO Transaction (Id, Active, StartDateTime, ExpiryDateTime) VALUES (@Id, 1, @StartDateTime, @ExpiryDateTime)";
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

        #endregion

        private void CreateTables()
        {
            var sql = "PRAGMA foreign_keys = ON;"+
                      "Create table IF NOT EXISTS Transactions" +
                      "(Id INTEGER PRIMARY KEY," +
                      " Active INTEGER NOT NULL," +
                      " StartDateTime DATETIME NOT NULL," +
                      " ExpiryDateTime DATETIME NOT NULL);" +

                      "Create table IF NOT EXISTS Queues" +
                      "(Id INTEGER PRIMARY KEY," +
                      " Name TEXT NOT NULL," +
                      " ExpiryDateTime DATETIME NOT NULL);" +

                      "Create TABLE IF NOT EXISTS Messages" +
                      "(Id INTEGER PRIMARY KEY," +
                      " QueueId INTEGER NOT NULL," +
                      " TransactionId INTEGER," +
                      " TransactionAction INTEGER," +
                      " AddDateTime DATETIME NOT NULL," +
                      " CloseDateTime DATETIME, " +
                      " Priority INTEGER NOT NULL, " +
                      " MaxRetries INTEGER NOT NULL, " +
                      " Retries INTEGER NOT NULL, " +
                      " ExpiryDate DateTime NOT NULL, " +
                      " CorrelationId INTEGER, " +
                      " GroupName TEXT, " +
                      " Data BLOB," +
                      " FOREIGN KEY(QueueId) REFERENCES Queues(Id)," +
                      " FOREIGN KEY(TransactionId) REFERENCES Transactions(Id));";


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

        private SortedList<string, int> LoadQueueList()
        {
            var sql = "SELECT Name, Id FROM Queues;";
            return new SortedList<string, int>(_con.Query(sql).ToDictionary(row => (string) row.Name, row => (int) row.Id));
        }

        public void CreateQueue(in String name)
        {
            var fixedName = name.Trim();
            if (fixedName.Length == 0)
                throw new ArgumentException(nameof(name), "Queue name cannot be empty");
            if (_queueList.ContainsKey(fixedName))
                    throw new Exception("Queue name already exists");
            var sql = "INSERT INTO Queues (Id, Name) VALUES (@Id, @Name);";
            var nextId = Interlocked.Increment(ref _queueId);
            _con.Execute(sql, new {Id = nextId, Name = fixedName });
            _queueList.Add(fixedName, nextId);
        }

        public void DeleteQueue(in String name)
        {
            var fixedName = name.Trim();
            if (fixedName.Length == 0)
                throw new ArgumentException(nameof(name), "Queue name cannot be empty");
            if (!_queueList.TryGetValue(fixedName, out var id))
                throw new Exception("Queue name not found");
            var trans = _con.BeginTransaction();

            //TODO: Rollback queue transactions that were being used in message for this queue
            //TODO: Delete Messages

            //Delete Queue
            var sql = "DELETE FROM Queues WHERE Id = @Id;";
            _con.Execute(sql, transaction: trans, param: new { Id = id });
            trans.Commit();
            _queueList.Delete(fixedName);
        }

        #endregion
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

    internal class QueueModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    internal enum TransactionAction
    {

        Unknown = 0,
        Add = 1,
        Pull = 2
    }
}
