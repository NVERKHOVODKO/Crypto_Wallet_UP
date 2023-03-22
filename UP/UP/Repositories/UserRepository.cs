﻿using System.Data;
using System.Xml.Serialization;
using Npgsql;
using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using UP.Models;
using System.Net;

namespace UP.Repositories;

public class UserRepository: RepositoryBase
{
    public Models.User GetUserById(int userId)
    {
        NpgsqlConnection connection = new NpgsqlConnection(connectionString);
        Models.User user;
        connection.Open();
        var sql = "SELECT * FROM users WHERE id = @userId";
        using (var command = new NpgsqlCommand(sql, connection))
        {
            command.Parameters.AddWithValue("userId", userId);
                
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    int id = reader.GetInt32(0);
                    String login = reader.GetString(1);
                    String password = reader.GetString(2);
                    String email = reader.GetString(3);
                    DateTime creationDate = reader.GetDateTime(4);
                    DateTime modificationDate = reader.GetDateTime(5);
                    Boolean isDeleted = reader.GetBoolean(6);
                    Boolean isBlocked = reader.GetBoolean(7);
                    int roleId = reader.GetInt32(8);
                    user = new Models.User(id, login, password, email, creationDate, 
                        modificationDate, isDeleted, isBlocked, roleId);
                    return user;
                }
            }
        }
        connection.Close(); // закрываем подключение
        return null;
    }
    
    public void WriteNewUserToDatabase(String login, String password, String email)
        {
            DateTime curDateTime = DateTime.Now;
            DateTime modificationDateTime = DateTime.Now;
            var user = new Models.User(1, login, password, email, curDateTime, modificationDateTime, false, false, 1);
            using var connection = new NpgsqlConnection(connectionString);

            var sql = "INSERT INTO users (login, password, email, creation_date, modification_date, is_deleted, is_blocked, role_id) " +
                      "VALUES (@login, @password, @email, @creation_date, @modification_date, @is_deleted, @is_blocked, @role_id)";

            using var command = new NpgsqlCommand(sql, connection);

            command.Parameters.AddWithValue("@login", user.Login);
            command.Parameters.AddWithValue("@password", user.Password);
            command.Parameters.AddWithValue("@email", user.Email);
            command.Parameters.AddWithValue("@creation_date", user.CreationData);
            command.Parameters.AddWithValue("@modification_date", user.ModificationDate);
            command.Parameters.AddWithValue("@is_deleted", user.IsDeleted);
            command.Parameters.AddWithValue("@is_blocked", user.IsBlocked);
            command.Parameters.AddWithValue("@role_id", user.RoleId);

            OpenConnection(connection);
            command.ExecuteNonQuery();
            CloseConnection(connection);
        }
        
        public void SaveAccountLoginHistory(int id)
        {
            try
            {
                string ipAddress = Dns.GetHostEntry(Dns.GetHostName()).AddressList
                    .FirstOrDefault(ip => ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    ?.ToString();
                var sql = "INSERT INTO login_history (ip, date, user_id) " +
                          "VALUES (@ip, @date, @user_id)";
                NpgsqlConnection connection = new NpgsqlConnection(connectionString);
                using var command = new NpgsqlCommand(sql, connection);
                command.Parameters.AddWithValue("@ip", ipAddress);
                command.Parameters.AddWithValue("@date", DateTime.Now);
                command.Parameters.AddWithValue("@user_id", id);
                OpenConnection(connection);
                command.ExecuteNonQuery();
                CloseConnection(connection);
            }
            catch (Exception e)
            {
                Console.WriteLine("Error. Can't update login history");
            }
        }

        public List<Models.Coin> GetUserCoins(int userId)
        {
            using (var connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT c.id,c.quantity, c.shortname " +
                               "FROM coins c " +
                               "INNER JOIN l_users_coins l ON l.coin_id = c.id " +
                               "WHERE l.user_id = @userId";
                List<Models.Coin> coins = new List<Coin>();
                using (var cmd = new NpgsqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@userId", userId);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            int coinId = reader.GetInt32(0);
                            double coinQuantity = reader.GetDouble(1);
                            string shortname = reader.GetString(2);
                            Coin coin = new Coin(coinId, coinQuantity, shortname);
                            coins.Add(coin);
                        }
                    }
                }
                connection.Close();
                return coins;
            }
        }

        public double GetCoinQuantityInUserWallet(int userId, string coinShortname)
        {
            try
            {
                using (var connection = new NpgsqlConnection(connectionString))
                {
                    connection.Open();
                    string query = "SELECT c.id,c.quantity, c.shortname " +
                                   "FROM coins c " +
                                   "INNER JOIN l_users_coins l ON l.coin_id = c.id " +
                                   "WHERE l.user_id = @userId AND c.shortname = @coinShortname";
                    List<Models.Coin> coins = new List<Coin>();
                    using (var cmd = new NpgsqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@userId", userId);
                        cmd.Parameters.AddWithValue("@coinShortname", coinShortname);
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                int coinId = reader.GetInt32(0);
                                double coinQuantity = reader.GetDouble(1);
                                string shortname = reader.GetString(2);
                                Coin coin = new Coin(coinId, coinQuantity, shortname);
                                coins.Add(coin);
                            }
                        }
                    }
                    connection.Close();
                    return coins[0].quantity;
                }

                /*string sql = "SELECT c.quantity " +
                             "FROM coins c " +
                             "INNER JOIN l_users_coins l ON l.coin_id = c.id " +
                             "WHERE l.user_id = @userId AND c.shortname = @coinShortname";
                double quantity = 0;
                var connection = new NpgsqlConnection(connectionString);
                using var command = new NpgsqlCommand(sql, connection);
                command.Parameters.AddWithValue("@user_id", userId);
                command.Parameters.AddWithValue("@shortname", coinShortname);
                OpenConnection(connection);
                quantity = Convert.ToDouble(command.ExecuteScalar());
                CloseConnection(connection);
                return quantity;*/
            }
            catch (Exception)
            {
                return 0.0;
            }
        }

        public bool IsLoginUnique(String login)
        {
            using var connection = new NpgsqlConnection(connectionString);
            var sql = "SELECT COUNT(*) FROM users WHERE login = @login";
            using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("@login", login);
            OpenConnection(connection);
            int count = Convert.ToInt32(command.ExecuteScalar());
            CloseConnection(connection);
            return Convert.ToBoolean(count);
        }
        
        public void EditUser(int id,Models.User user)
        {
            using var connection = new NpgsqlConnection(connectionString);
            var sql = "UPDATE users SET (login, password, email, creation_date, modification_date, is_deleted, is_blocked, role_id) " +
                      "= (@login, @password, @email, @creation_date, @modification_date, @is_deleted, @is_blocked, @role_id) WHERE id = @id";
            using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("@login", user.Login);
            command.Parameters.AddWithValue("@password", user.Password);
            command.Parameters.AddWithValue("@email", user.Email);
            command.Parameters.AddWithValue("@creation_date", user.CreationData);
            command.Parameters.AddWithValue("@modification_date", DateTime.Now);
            command.Parameters.AddWithValue("@is_deleted", user.IsDeleted);
            command.Parameters.AddWithValue("@is_blocked", user.IsBlocked);
            command.Parameters.AddWithValue("@role_id", user.RoleId);
            OpenConnection(connection);
            command.ExecuteNonQuery();
            CloseConnection(connection);
        }
        
        public void SetUserStatusDel(int id, bool isDeleted)
        {
            using var connection = new NpgsqlConnection(connectionString);
            var sql = "UPDATE users SET is_deleted = @isDeleted WHERE id = @id";
            using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("@id", id);
            command.Parameters.AddWithValue("@isDeleted", isDeleted);
            OpenConnection(connection);
            command.ExecuteNonQuery();
            CloseConnection(connection);
            UpdateModificationDate(id);
        }
        
        public void SetUserStatusBlock(int id, bool isBlocked)
        {
            using var connection = new NpgsqlConnection(connectionString);
            var sql = "UPDATE users SET is_blocked = @isBlocked WHERE id = @id";
            using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("@id", id);
            command.Parameters.AddWithValue("@isBlocked", isBlocked);
            OpenConnection(connection);
            command.ExecuteNonQuery();
            CloseConnection(connection);
            UpdateModificationDate(id);
        }
        
        public void DeleteUser(int id)
        {
            using var connection = new NpgsqlConnection(connectionString);
            var sql = "DELETE users WHERE id = @id";
            using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("@id", id);
            OpenConnection(connection);
            command.ExecuteNonQuery();
            CloseConnection(connection);
            UpdateModificationDate(id);
        }
        
        public void ChangeUserName(int id, string newLogin)
        {
            using var connection = new NpgsqlConnection(connectionString);
            var sql = "UPDATE users SET login = @newLogin WHERE id = @id";
            using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("@id", id);
            command.Parameters.AddWithValue("@newLogin", newLogin);
            OpenConnection(connection);
            command.ExecuteNonQuery();
            CloseConnection(connection);
            UpdateModificationDate(id);
        }
        
        public void ChangePassword(int id, string password)
        {
            using var connection = new NpgsqlConnection(connectionString);
            var sql = "UPDATE users SET password = @password WHERE id = @id";
            using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("@id", id);
            command.Parameters.AddWithValue("@password", password);
            OpenConnection(connection);
            command.ExecuteNonQuery();
            CloseConnection(connection);
            UpdateModificationDate(id);
        }
        
        public List<Models.User> GetUserList()
        {
            NpgsqlConnection connection = new NpgsqlConnection(connectionString);
            Models.User user;
            connection.Open();
            var sql = "SELECT * FROM users";
            List<Models.User> users = new List<User>();
            using (var command = new NpgsqlCommand(sql, connection))
            {
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        int id = reader.GetInt32(0);
                        String login = reader.GetString(1);
                        String password = reader.GetString(2);
                        String email = reader.GetString(3);
                        DateTime creationDate = reader.GetDateTime(4);
                        DateTime modificationDate = reader.GetDateTime(5);
                        Boolean isDeleted = reader.GetBoolean(6);
                        Boolean isBlocked = reader.GetBoolean(7);
                        int roleId = reader.GetInt32(8);
                        users.Add(new Models.User(id, login, password, email, creationDate, 
                            modificationDate, isDeleted, isBlocked, roleId));
                    }
                }
            }
            connection.Close();
            return users;
        }
        
        public void UpdateModificationDate(int id)
        {
            using var connection = new NpgsqlConnection(connectionString);
            var sql = "UPDATE users SET modification_date = @modification_date WHERE id = @id";
            using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("@id", id);
            command.Parameters.AddWithValue("@modification_date", DateTime.Now);
            OpenConnection(connection);
            command.ExecuteNonQuery();
            CloseConnection(connection);
        }
        
        public Models.User Login(String login, String password)
        {
            NpgsqlConnection connection = new NpgsqlConnection(connectionString);
            Models.User user;
            connection.Open();
            var sql = "SELECT * FROM users WHERE login = @login AND password = @password";
            using (var command = new NpgsqlCommand(sql, connection))
            {
                command.Parameters.AddWithValue("login", login);
                command.Parameters.AddWithValue("password", password);
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        int id = reader.GetInt32(0);
                        login = reader.GetString(1);
                        password = reader.GetString(2);
                        String email = reader.GetString(3);
                        DateTime creationDate = reader.GetDateTime(4);
                        DateTime ModificationDate = reader.GetDateTime(5);
                        Boolean isDeleted = reader.GetBoolean(6);
                        Boolean isBlocked = reader.GetBoolean(7);
                        int roleID = reader.GetInt32(8);
                        user = new Models.User(id, login, password, email, creationDate, 
                            ModificationDate, isDeleted, isBlocked, roleID);
                        connection.Close();
                        return user;
                    }
                }
            }
            connection.Close();
            return null;
        }
}