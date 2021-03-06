using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Text;

namespace CustomsQueueBot.Core.Database
{
    public struct Player
    {
        public ulong DiscordID { get; set; } 
        public string InGameName { get; set; }
        public int PlayerLevel { get; set; }
        public int PlaysFrontline { get; set; } // From here to IsBanned are integers for booleans. 0 = False | 1 = True
        public int PlaysSupport { get; set; }
        public int PlaysFlank { get; set; }
        public int PlaysDamage { get; set; }
        public int IsBanned { get; set; }
        public string BannedReason { get; set; }
    }
    public class Database
    {
        private SqliteDBContext Db = new SqliteDBContext();
        public Database()
        {
            CreateTable();
        }

        private void CreateTable()
        {
            string query = "CREATE TABLE IF NOT EXISTS users (id varchar(24), ign varchar(24), level int, isTank int, " +
                "isSupp int, isFlank, isDmg int, banned int, reason varchar(50)";

            SQLiteCommand command = new SQLiteCommand(query, Db.Connection);
            Db.OpenConnection();
            command.Prepare();
            command.ExecuteNonQuery();
            Db.CloseConnection();

        }

        public void CreateUser(Player player)
        {
            string query = "INSERT INTO users (id, ) VALUES (@id, )";
            SQLiteCommand command = new SQLiteCommand(query, Db.Connection);
            Db.OpenConnection();
            command.Parameters.AddWithValue("@id", player.DiscordID);
            command.Parameters.AddWithValue("@ign", player.InGameName);
            command.Parameters.AddWithValue("@level", player.PlayerLevel);
            command.Parameters.AddWithValue("@isTank", player.PlaysFrontline);
            command.Parameters.AddWithValue("@isSupp", player.PlaysSupport);
            command.Parameters.AddWithValue("@isFlank", player.PlaysFlank);
            command.Parameters.AddWithValue("@isDmg", player.PlaysDamage);
            command.Parameters.AddWithValue("@banned", player.IsBanned);
            command.Parameters.AddWithValue("@reason", player.BannedReason);
            command.Prepare();
            command.ExecuteNonQuery();
            Db.CloseConnection();

        }

        public Player GetPlayerById(ulong id)
        {
            string query = $"SELECT * FROM users WHERE id = {id}";
            SQLiteCommand command = new SQLiteCommand(query, Db.Connection);
            Db.OpenConnection();
            command.Prepare();
            SQLiteDataReader Result = command.ExecuteReader();
            Player player = new Player();
            
            if(Result.HasRows)
                while (Result.Read())
                {
                    player.DiscordID = id;
                    player.InGameName = Result["ign"].ToString();
                    player.PlayerLevel = Convert.ToInt32(Result["level"].ToString());
                    player.PlaysFrontline = Convert.ToInt32(Result["isTank"].ToString());
                    player.PlaysSupport = Convert.ToInt32(Result["isSupp"].ToString());
                    player.PlaysFlank = Convert.ToInt32(Result["isFlank"].ToString());
                    player.PlaysDamage = Convert.ToInt32(Result["isDmg"].ToString());
                    player.IsBanned = Convert.ToInt32(Result["banned"].ToString());
                    player.BannedReason = Result["reason"].ToString();
                }

            return player;
        }

        public bool UserExists(ulong id)
        {
            string query = $"SELECT * FROM users WHERE id = {id}";
            SQLiteCommand command = new SQLiteCommand(query, Db.Connection);
            Db.OpenConnection();
            command.Prepare();
            SQLiteDataReader Result = command.ExecuteReader();

            if (Result.HasRows)
                return true;
            else
                return false;
        }

    }
}
