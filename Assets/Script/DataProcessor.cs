using UnityEngine;
using System;
using System.IO;
using System.Collections;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

[System.Serializable]
public class DataProcessor
{
	FileStream stream;
	public Hashtable playerInformation;
	BinaryFormatter binaryFormatter;

	// constuctor - no parameter
	public DataProcessor()
	{
		stream = null;
		binaryFormatter = new BinaryFormatter();
		playerInformation = new Hashtable();
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
		Player newPlayer = new Player( playerID, playerPassword );

		// add player information -> hashtable
		try
		{
			playerInformation.Add( newPlayer.ID, newPlayer );
			stream = new FileStream( "PlayerData.data", FileMode.Create );
			binaryFormatter.Serialize( stream, playerInformation );
		}
		catch ( NullReferenceException e )
		{
			// data null error
			Debug.Log( e.Message );
			Debug.Log( "Server : Null Reference Exception - DataProcessor : JoinPlayer (add player information in hashtable)" );
			result = "Wrong data";
			return false;
		}
		catch ( ArgumentException e )
		{
			// same key error
			// serialize stream null error
			Debug.Log( e.Message );
			Debug.Log( "Server : Arguement Exception - DataProcessor : JoinPlayer (add player information in hashtable)" );
			result = "ID -> Exist ID or Wrong ID"; 
			return false;
		}
		catch ( IOException e )
		{
			// file stream error
			Debug.Log( e.Message );
			Debug.Log( "Server : IO Exception - DataProcessor : JoinPlayer (add player information in hashtable)" );
			result = "Server Error - Require check file stream";
			return false;
		}
		catch ( SerializationException e )
		{
			// serialize parameter error
			Debug.Log( e.Message );
			Debug.Log( "Server : Serialization Exception - DataProcessor : JoinPlayer (add player information in hashtable)" );
			result = "Server Error - Require check binary fomatter";
			return false;
		}

		// success add new player
		result = " Join Success";
		return true;
	}
}
