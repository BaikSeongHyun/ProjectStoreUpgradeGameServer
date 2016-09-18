using MySql;
using MySql.Data;
using MySql.Data.MySqlClient;
using System;
using System.Data;
using System.IO;
using System.Collections;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

public class DatabaseConnector
{
	// private field
	private MySqlConnection connection = null;
	private MySqlCommand command = null;
	private string connectString;
	
	// static field
	static DatabaseConnector instance;
		
	// property
	public static DatabaseConnector Instance
	{
		get { return instance; }
	}
	
	// single tone constructor
	static DatabaseConnector ()
	{
		instance = new DatabaseConnector();	
	}
	
	// constructor for initialize
	private DatabaseConnector ()
	{
		try
		{
			connection = new MySqlConnection(connectString);	
			connection.Open();
			Debug.Log( "Connection State : " + connection.State );
			
		}
		catch (Exception e)
		{
			Debug.Log( e.ToString() );
			Debug.Log( "Server : Exception - DatabaseConnector : MySql server connection failed" ); 
		}
	}
}
