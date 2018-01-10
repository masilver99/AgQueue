using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
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

        private Object _transLock = new Object();
        private Object _queueLock = new Object();

        private SortedList<string, int> _queueList;


        //Settings
        //How long until a transcation expires and it automatically rolled back
        private int _expiryTimeInMinutes = 30;

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
            _queueList = LoadQueueList();
        }

        #region Transactions
        public Transaction StartTransaction()
        {
            var sql = "INSERT INTO Transaction (Id, Active, StartDateTime, ExpiryDateTime) VALUES (@Id, 1, @StartDateTime, @ExpiryDateTime)";
            lock (_transLock)
            {
                _transId++;
            }
            _con.Execute(sql, new { StartDateTime = DateTime.Now, ExpiryDateTime = DateTime.Now.AddMinutes(_expiryTimeInMinutes), Id = _transId });
            return new Transaction() {Id = _transId, Api = this};
        }

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
            var sql = "Create table IF NOT EXISTS Transactions" +
                      "(Id INTEGER PRIMARY KEY," +
                      " Active INTEGER NOT NULL," +
                      " StartDateTime DATETIME NOT NULL," +
                      " ExpiryDateTime DATETIME NOT NULL);"+

                      "Create table IF NOT EXISTS Queues" +
                      "(Id INTEGER PRIMARY KEY," +
                      " Name TEXT NOT NULL" +
                      " ExpiryDateTime DATETIME NOT NULL);";
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
            lock (_queueLock)
            {
                _queueId++;
            }
            _con.Execute(sql, new {Id = _queueId, Name = fixedName });
            _queueList.Add(fixedName, _queueId);
        }

        public void DeleteQueue(in String name)
        {
            var fixedName = name.Trim();
            if (fixedName.Length == 0)
                throw new ArgumentException(nameof(name), "Queue name cannot be empty");
            if (!_queueList.TryGetValue(fixedName, out int id))
                throw new Exception("Queue name not found");
            //Start db transaction
            //Rollback queue transactions that were being used in message for this queue
            //Delete Messages
            //Delete Queue
            var sql = "DELETE FROM Queues WHERE Id = @Id;";
            _con.Execute(sql, new { Id = id });
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
        public int Id { get; set; }
    }

    internal class QueueModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
}
