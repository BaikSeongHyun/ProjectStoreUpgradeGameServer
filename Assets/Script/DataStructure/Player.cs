using System;

public class Player
{
	//key value
	string playerID;

	//dependant value
	string password;
	int availableMoney;
	Store[] haveStore;
	Item[] haveItem;
	DecorateObject[] haveDecorateObject;

	// property
	public string ID { get { return playerID; } }

	//constructor -> join
	public Player( string _playerID, string _password )
	{
		playerID = _playerID;
		password = _password;
	}


}


