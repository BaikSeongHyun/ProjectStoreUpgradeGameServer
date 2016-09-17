using UnityEngine;
using System;
using System.IO;
using System.Collections;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

[System.Serializable]
public class DataProcessor
{
	// file streaming
	FileStream stream;
	BinaryFormatter binaryFormatter;

	// player data process
	public Hashtable playerInformation;
	public Hashtable loginInformation;

	// constuctor - no parameter
	public DataProcessor()
	{
		stream = null;
		binaryFormatter = new BinaryFormatter();
		playerInformation = new Hashtable();
		loginInformation = new Hashtable();
	}

	// constructor - file stream information parameter
	public DataProcessor( string dataPath, FileMode fileMode )
	{
		stream = new FileStream( dataPath, fileMode );
		binaryFormatter = new BinaryFormatter();

		if( stream.Length != 0 )
			playerInformation = (Hashtable) binaryFormatter.Deserialize( stream );
		else
			playerInformation = new Hashtable();
	}

	public bool JoinPlayer( string playerID, string playerPassword, out string result )
	{
		// add player information -> hashtable ( for player Information )
		Player newPlayer = new Player( playerID, playerPassword );
		playerInformation.Add( newPlayer.ID, newPlayer );

		stream.Close();

		// create file -> Write new file
		try
		{
			stream = new FileStream( "PlayerData.data", FileMode.Create );
		}
		catch ( IOException e )
		{
			Debug.Log( e.Message );
			Debug.Log( "Server : IO Exception - DataProcessor : JoinPlayer (create file -> Sharing violation on path)" );
			result = "Do not create file";
			return false;
		}
	
		// data serialize
		try
		{
			binaryFormatter.Serialize( stream, playerInformation );
			stream.Close();
		}
		catch ( NullReferenceException e )
		{
			// data null error
			Debug.Log( e.Message );
			Debug.Log( "Server : Null Reference Exception - DataProcessor : JoinPlayer (data serialize)" );
			result = "Wrong data";
			return false;
		}
		catch ( ArgumentException e )
		{
			// same key error
			// serialize stream null error
			Debug.Log( e.Message );
			Debug.Log( "Server : Arguement Exception - DataProcessor : JoinPlayer (data serialize)" );
			result = "ID -> Exist ID or Wrong ID"; 
			return false;
		}
		catch ( SerializationException e )
		{
			// serialize parameter error
			Debug.Log( e.Message );
			Debug.Log( "Server : Serialization Exception - DataProcessor : JoinPlayer (data serialize)" );
			result = "Server Error - Require check binary fomatter";
			return false;
		}
		catch ( IOException e )
		{
			Debug.Log( e.Message );
			Debug.Log( "Server : IO Exception - DataProcessor : JoinPlayer (data serialize)" );
			result = "Do not create file";
			return false;
		}

		// stream reopen
		try
		{
			stream = new FileStream( "DataFile.Data", FileMode.Open );
		}
		catch ( IOException e )
		{
			Debug.Log( e.Message );
			Debug.Log( "Server : IO Exception - DataProcessor : JoinPlayer (stream re open)" );
			result = "Do not open file";
			return false;
		}

		// success add new player
		result = " Join Success";
		return true;
	}

	public bool LoginPlayer( string playerID, string playerPassword, out string result )
	{
		// check id & password
		try
		{
			Player temp;
			temp = (Player) playerInformation[playerID];
			if( temp.Password == playerPassword )
			{
				loginInformation.Add( playerID, temp );

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
		catch ( NotSupportedException e )
		{
			// id is not exist
			Debug.Log( e.Message );
			Debug.Log( "Server : NotSupportedException - DataProcessor : LoginPlayer ( check id & password)" );
			result = "ID is not exist";
			return false;
		}
		catch ( ArgumentException e )
		{
			Debug.Log( e.Message );
			Debug.Log( "Server : ArgumentException - DataProcessor : LoginPlayer ( check id )" );
			result = "ID is already Login this server";
			return false;
		}
	}

	public bool PlayerMakeStore( string playerID, string store, out string resultString )
	{

		resultString = "Make Success";
		return true;	
	}
}
