using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Text;

namespace CustomsQueueBot.Core.Database
{
    public class SqliteDBContext
    {

        public SQLiteConnection Connection;
        public SqliteDBContext()
        {
            Connection = new SQLiteConnection(@"Data Source:Resources\Database.sqlite3"); // @ notation removes escape character functions
            if (!File.Exists("Resources/Database.sqlite3"))
            {
                SQLiteConnection.CreateFile("Resources / Database.sqlite3");
                Console.WriteLine("Databse was created.");
            }
        }

        public void OpenConnection()
        {
            if (Connection.State != System.Data.ConnectionState.Open)
                Connection.Open();
        }

        public void CloseConnection()
        {
            if (Connection.State != System.Data.ConnectionState.Closed)
                Connection.Close();
        }




    }
}
