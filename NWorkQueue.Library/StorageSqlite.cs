using System;
using System.Collections.Generic;
using System.Text;
using Dapper;
using Microsoft.Data.Sqlite;

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
                CreateTables();
                _transId = GetTransId();
                _messageId = GetMessageId();
                _queueId = GetQueueId();
                _queueList.Reload(LoadQueueList());
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
                " ExpiryDateTime DATETIME NOT NULL);" +

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

    }
}
