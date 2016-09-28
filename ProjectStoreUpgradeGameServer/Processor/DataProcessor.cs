using System;
using System.Collections.Generic;
using System.Net.Sockets;

using MySql.Data.MySqlClient;

public class DataProcessor
{
    // network Processor
    NetworkProcessor networkProcessor;

    // mysql connect
    string connectString;
    MySqlConnection sqlConnection;
    MySqlCommand sqlCommand;

    // constuctor - no parameter
    public DataProcessor(NetworkProcessor netpro)
    {
        // connect network processor
        networkProcessor = netpro;

        // connect database
        sqlCommand = new MySqlCommand();
        try
        {
            connectString = "Server=127.0.0.1;Database=gameschema;Uid=root;Pwd=root";
            sqlConnection = new MySqlConnection(connectString);
            sqlConnection.Open();
        }
        catch (Exception e)
        {
            Console.WriteLine(e.StackTrace);
            Console.WriteLine("Server : Exception - DataProcessor : Database connect fail");
        }
        finally
        {
            sqlConnection.Close();
        }

        Console.WriteLine("Success Database Connect");
    }

    // processing join
    public bool JoinPlayer(string playerID, string playerPassword, out string result)
    {
        string query;

        // write database -> create player id : in player table
        // ( if primary key is exist -> throw exception & return false )
        try
        {
            sqlConnection.Open();
            query = "INSERT INTO player values ( '" + playerID + "', '" + playerPassword + "', '" + playerID + "' ," + 0 + ")";
            Console.WriteLine(query);
            sqlCommand = new MySqlCommand(query, sqlConnection);
            sqlCommand.ExecuteNonQuery();
        }
        catch (MySqlException e)
        {
            // id exist
            if (e.ErrorCode == -2147467259)
            {
                Console.WriteLine("Server : MySqlException - DataProcessor : JoinPlayer ( create id )");
                result = "ID is already exist";
                sqlConnection.Close();
                return false;
            }
            Console.WriteLine(e.StackTrace);
            Console.WriteLine(e.Message);
            Console.WriteLine("Server : MySqlException - DataProcessor : JoinPlayer ( create id )");
            result = "Database Connect fail ( create id )";
            sqlConnection.Close();
            return false;
        }
        finally
        {
            sqlConnection.Close();
        }

        // success add new player
        result = " Join Success : " + playerID;
        return true;
    }

    // processing login
    public bool LoginPlayer(Socket clientSocket, string playerID, string playerPassword, out string result)
    {
        Player temp = new Player();
        string query;

        // check id -> data load
        // load success -> has been join
        try
        {
            // connect database
            query = "SELECT * FROM player WHERE playerid = '" + playerID + "'";
            sqlCommand = new MySqlCommand(query, sqlConnection);
            sqlConnection.Open();
            MySqlDataReader sqlReader = sqlCommand.ExecuteReader();
            sqlReader.Read();

            // data input
            temp.ID = (string)sqlReader["playerid"];
            temp.Password = (string)sqlReader["password"];
            temp.Name = (string)sqlReader["playername"];
            temp.Money = (int)sqlReader["money"];
        }
        catch (MySqlException e)
        {
            //data is empty    
            if (e.ErrorCode == -2147467259)
            {
                Console.WriteLine("Server : MySqlException - DataProcessor : JoinPlayer ( create id )");
                result = "ID is not exist";
                sqlConnection.Close();
                return false;
            }

            Console.WriteLine(e.StackTrace);
            Console.WriteLine(e.Message);
            Console.WriteLine("Server : SQLException - DataProcessor : LoginPlayer ( check id )");
            result = "Server database error";
            sqlConnection.Close();
            return false;
        }
        finally
        {
            sqlConnection.Close();
        }

        // check id & password - login
        try
        {
            if (temp.Password == playerPassword)
            {
                networkProcessor.loginInformation.Add(playerID, temp);
                networkProcessor.loginSocketInformation.Add(clientSocket, playerID);
                // success login
                result = "Login Success";
                return true;
            }
            else
            {
                result = "Wrong Password";
                return false;
            }
        }
        catch (ArgumentException e)
        {
            Console.WriteLine(e.Message);
            Console.WriteLine("Server : ArgumentException - DataProcessor : LoginPlayer ( check id )");
            result = "ID is already Login this server";
            return false;
        }
        catch (NullReferenceException e)
        {
            Console.WriteLine(e.Message);
            Console.WriteLine("Server : NullReferenceException - DataProcessor : LoginPlayer ( check id )");
            result = "Database connection failed";
            return false;
        }
    }

    // processing game data output
    public void GetGameData(string playerID, List<StoreData> storeDataSet, List<ItemData> itemDataSet)
    {
        string query;       

        // get data -> store data
        try
        {
            query = "SELECT * FROM store WHERE playerid = '" + playerID + "'";
            sqlCommand = new MySqlCommand(query, sqlConnection);
            sqlConnection.Open();
            MySqlDataReader sqlReader = sqlCommand.ExecuteReader();

            // data read
            while (sqlReader.Read())
            {
                // data initalize
                StoreData tempStoreData = new StoreData();

                // data set
                tempStoreData.storeID = (string)sqlReader["storeid"];
                tempStoreData.playerID = (string)sqlReader["playerid"];
                tempStoreData.storeName = (string)sqlReader["storename"];
                tempStoreData.storeType = (byte)((int)sqlReader["storetype"]);
                tempStoreData.step = (byte)((int)sqlReader["step"]);
                tempStoreData.presentEXP = (float)sqlReader["presentEXP"];
                tempStoreData.requireEXP = (float)sqlReader["requireEXP"];

                // add database data in store data list
                storeDataSet.Add(tempStoreData);
            }
        }
        catch (MySqlException e)
        {
            Console.WriteLine(e.StackTrace);
            Console.WriteLine(e.Message);
        }
        finally
        {
            sqlConnection.Close();
        }

        // get data -> item data
        try
        {
            query = "SELECT * FROM item WHERE playerid = '" + playerID + "'";
            sqlCommand = new MySqlCommand(query, sqlConnection);
            sqlConnection.Open();
            MySqlDataReader sqlReader = sqlCommand.ExecuteReader();

            // data read
            while (sqlReader.Read())
            {
                // data intialize
                ItemData tempItemData = new ItemData();

                // data set
                tempItemData.itemID = (string)sqlReader["itemid"];
                tempItemData.playerID = (string)sqlReader["playerid"];
                tempItemData.storeType = (byte)((int)sqlReader["storetype"]);
                tempItemData.itemName = (string)sqlReader["itemName"];
                tempItemData.count = (short)((int)sqlReader["count"]);
                tempItemData.price = (int)sqlReader["price"];
                tempItemData.isSell = (bool)sqlReader["isSell"];
                tempItemData.sellPrice = (int)sqlReader["sellprice"];
                tempItemData.sellCount = (short)((int)sqlReader["sellcount"]);

                // add database data in item data list
                itemDataSet.Add(tempItemData);
            }
        }
        catch (MySqlException e)
        {
            Console.WriteLine(e.StackTrace);
            Console.WriteLine(e.Message);
        }
        finally
        {
            sqlConnection.Close();
        }
    }

    // processing create store
    public bool StoreCreate(StoreCreateRequestData inputData, StoreData outputData, out string resultString)
    {
        string query;

        // insert store data in database store table
        try
        {
            query = "INSERT INTO store values ( '" + inputData.storeID + "', '" + inputData.playerID + "', '" + inputData.storeName + "', " + inputData.storeType.ToString() + ", 1, 0, 0 )";
            Console.WriteLine(query);
            sqlCommand = new MySqlCommand(query, sqlConnection);
            sqlConnection.Open();
            sqlCommand.ExecuteNonQuery();
        }
        catch (MySqlException e)
        {
            // id exist
            if (e.ErrorCode == -2147467259)
            {
                Console.WriteLine("Server : MySqlException - DataProcessor : StoreCreate ( insert data )");
                resultString = "Store is already exist";
                sqlConnection.Close();
                return false;
            }
            Console.WriteLine(e.StackTrace);
            Console.WriteLine(e.Message);
            Console.WriteLine("Server : SQLException - DataProcessor : StoreCreate ( insert data )");
            resultString = "Server database error";
            sqlConnection.Close();
            return false;
        }
        finally
        {
            sqlConnection.Close();
        }

        // create data send
        try
        {
            query = "SELECT * FROM store WHERE playerid = '" + inputData.playerID + "' AND storeid = '" + inputData.storeID + "'";
            sqlCommand = new MySqlCommand(query, sqlConnection);
            sqlConnection.Open();
            MySqlDataReader sqlReader = sqlCommand.ExecuteReader();

            // data read
            while (sqlReader.Read())
            {
                // data set
                outputData.storeID = (string)sqlReader["storeid"];
                outputData.playerID = (string)sqlReader["playerid"];
                outputData.storeName = (string)sqlReader["storename"];
                outputData.storeType = (byte)((int)sqlReader["storetype"]);
                outputData.step = (byte)((int)sqlReader["step"]);
                outputData.presentEXP = (float)sqlReader["presentEXP"];
                outputData.requireEXP = (float)sqlReader["requireEXP"];
            }
        }
        catch (MySqlException e)
        {
            Console.WriteLine(e.StackTrace);
            Console.WriteLine(e.Message);
        }
        finally
        {
            sqlConnection.Close();
        }

        resultString = "Create Store Success";
        return true;
    }


    // processing create item
    public bool ItemCreate(string playerID, string itemID, short count, out string resultString)
    {
        string query;
        Item itemData = DataTable.Instance.FindItemUseID(itemID);

        // insert item data in database item table
        try
        {
            query = "INSERT INTO item values ( '" + itemID + "', '" + playerID + "', " + itemData.StoreType.ToString() + ", '"
                + itemData.Name + "', " + count.ToString() + ", " + itemData.Price.ToString() + ", " + itemData.OnSell.ToString() + ", "
                + itemData.SellPrice.ToString() + ", " + itemData.SellCount.ToString() + ")";
            Console.WriteLine(query);
            sqlCommand = new MySqlCommand(query, sqlConnection);
            sqlCommand.ExecuteNonQuery();
        }
        catch (MySqlException e)
        {
            // id exist
            if (e.ErrorCode == -2147467259)
            {
                Console.WriteLine("Server : MySqlException - DataProcessor : JoinPlayer ( create id )");
                resultString = "Item is already exist";
                sqlConnection.Close();
                return false;
            }
            Console.WriteLine(e.StackTrace);
            Console.WriteLine(e.Message);
            Console.WriteLine("Server : SQLException - DataProcessor : LoginPlayer ( check id )");
            resultString = "Server database error";
            sqlConnection.Close();
            return false;
        }
        finally
        {
            sqlConnection.Close();
        }

        resultString = "Create Store Success";
        return true;
    }
}
