using UnityEngine;
using System.Collections;
using System;

enum Command : byte
{
    Disconnect = 0,
    Connect = 1,
    Chat = 2,
    Move = 3,
    PositionInList = 4,
    CastAOEStart = 5,
    CastTargetStart = 6,
    CastNoTargetStart = 7,
    CastAOEEnd = 8
};

public class gameScript : MonoBehaviour {

    //Network
    public networkScript myNetwork;
    //Game
    private Command recCommand;
    private uint recPlayer;
    private float recX, recY, recZ, recRotW, recRotX, recRotY, recRotZ;
    private uint playerCount = 0;
    private playerScript[] playerList;
    private uint[] nextSlot;
    private string outMessage;
    uint recPlayerPosInList;

    public void processMessage(int recConnectionId, string[] message) {
        recCommand = (Command)byte.Parse(message[0]);

        switch (recCommand)
        {
            case Command.Disconnect:
                //Ein Spieler ist disconnected
                //Das wird eigentlich nicht vorkommen, weil wir schon im networkScript die disconnectmessage behandeln
                break;
            case Command.Connect:
                //Ein Spieler ist connected
                //TODO: Die playerId muss vermutlich aus der Datenbank kommen
                recPlayer = playerCount + 200;
                addPlayerToList(recConnectionId, recPlayer);
                break;
            case Command.Chat:
                //Ein Spieler will eine Chatnachricht verschicken
                recPlayer = uint.Parse(message[1]);
                recPlayerPosInList = uint.Parse(message[2]);
                break;
            case Command.Move:
                //Ein Spieler hat sich bewegt
                recPlayer = uint.Parse(message[1]);
                recPlayerPosInList = uint.Parse(message[2]);
                recX = float.Parse(message[3]);
                recY = float.Parse(message[4]);
                recZ = float.Parse(message[5]);
                recRotW = float.Parse(message[6]);
                recRotX = float.Parse(message[7]);
                recRotY = float.Parse(message[8]);
                recRotZ = float.Parse(message[9]);
                movePlayer(recPlayer, recPlayerPosInList, recX, recY, recZ, recRotW, recRotX, recRotY, recRotZ);
                break;
            case Command.PositionInList:
                //Wird auch nicht vorkommen
                break;
            case Command.CastAOEStart:
                //Jemand sagt uns, dass er einen AOE-Cast startet
                recPlayer = uint.Parse(message[1]);
                recPlayerPosInList = uint.Parse(message[2]);
                recX = float.Parse(message[3]);
                recY = float.Parse(message[4]);
                recZ = float.Parse(message[5]);
                checkSpellCast(recPlayer, recPlayerPosInList, recX, recY, recZ);
                break;
            default:
                break;
        }
    }

    public void findLeaver(int recConnectionId)
    {
        //Einer ist raus
        for (uint i = 0; i < playerList.GetLength(0); i++)
        {
            if (playerList[i].getConnectionId() == recConnectionId)
            {
                removePlayerFromList(i);
            }
        }
    }

    private void checkSpellCast(uint recPlayer, uint recPlayerPosInList, float recX, float recY, float recZ)
    {
        playerScript castingPlayer = playerList[recPlayerPosInList];
        //schauen ob er der ist, fuer den er sich ausgibt
        if (castingPlayer.getPlayerId() == recPlayer)
        {
            //Checken ob er genug Mana hat
            if (castingPlayer.getCurrentMana() >= 20)
            {
                //Cast geht durch
                //Mana reduzieren
                castingPlayer.reduceRessource(true, 20);
                broadcastSpellEnd(recPlayerPosInList, recX, recY, recZ);
            }
        }
    }

    private void movePlayer(uint recPlayer, uint recPlayerPosInList, float recX, float recY, float recZ, float recRotW, float recRotX, float recRotY, float recRotZ)
    {
        if (checkIdentity(recPlayer, recPlayerPosInList))
        {
            playerList[recPlayerPosInList].setPosition(recX, recY, recZ);
            //Todo: Rotation im playerscript
            playerList[recPlayerPosInList].setRotation(recRotX);
            broadcastMovement(recPlayerPosInList, recX, recY, recZ, recRotW, recRotX, recRotY, recRotZ);
        }
    }

    //TODO: Vorher noch den Spellbeginn durchgeben, damit die ihre Animationen und so anpassen
    //TODO: Evtl fuer alle Spells fit machen mit val1, val2 usw. und command anpassbar
    private void broadcastSpellEnd(uint playerPosInList, float recX, float recY, float recZ)
    {
        for (uint i = 0; i < playerList.GetLength(0); i++)
        {
            if (playerList[i] == null)
            {
                continue;
            }
            outMessage = "8;" + playerPosInList + ";" + recX + ";" + (recY + 2f) + ";" + recZ;
            myNetwork.sendMessage(outMessage, playerList[i].getConnectionId());
        }
    }

    private void broadcastMovement(uint recPlayerPosInList, float recX, float recY, float recZ, float recRotW, float recRotX, float recRotY,float recRotZ)
    {
        for (uint i = 0; i < playerList.GetLength(0); i++)
        {
            if (playerList[i] == null)
            {
                continue;
            }
            if (playerList[i].getListPos() != recPlayerPosInList)
            {
                outMessage = "3;" + recPlayerPosInList + ";" + recX + ";" + recY + ";" + recZ + ";" + recRotW + ";" + recRotX + ";" + recRotY + ";" + recRotZ;
                myNetwork.sendMessage(outMessage, playerList[i].getConnectionId());
            }
        }
    }

    private void broadcastNewPlayer(uint positionToAddTo)
    {
        for (uint i = 0; i < playerList.GetLength(0); i++)
        {
            if (playerList[i] == null)
            {
                continue;
            }
            if (playerList[i].getListPos() != positionToAddTo)
            {
                //Den Anderen sagen, wer der Neue ist
                outMessage = "1;" + positionToAddTo;
                Debug.Log("Zeige Spieler " + i + " den neuen Spieler auf " + positionToAddTo);
                myNetwork.sendMessage(outMessage, playerList[i].getConnectionId());
                //Und dem Neuen diesen Spieler zeigen
                outMessage = "1;" + playerList[i].getListPos();
                Debug.Log("Zeige neuem Spieler " + positionToAddTo + " den Spieler auf " + playerList[i].getListPos());
                myNetwork.sendMessage(outMessage, playerList[positionToAddTo].getConnectionId());
            }
        }
    }

    private void broadcastLeftPlayer(uint positionToRemove)
    {
        for (uint i = 0; i < playerList.GetLength(0); i++)
        {
            if (playerList[i] == null)
            {
                continue;
            }
            outMessage = "0;" + positionToRemove;
            myNetwork.sendMessage(outMessage, playerList[i].getConnectionId());
        }
    }

    private void removePlayerFromList(uint recPlayerPosInList)
    {
        playerCount--;

        if (playerCount == 0)
        {
            resetNextSlotList();
        }
        else
        {
            //Merken, welche Position das nächste mal gefüllt werden muss, wenn diese Anzahl von Spielern vorhanden ist
            nextSlot[playerCount] = recPlayerPosInList;
        }
        playerList[recPlayerPosInList] = null;
        //An alle Broadcasten, dass er weg ist
        broadcastLeftPlayer(recPlayerPosInList);
    }

    private bool checkIdentity(uint recPlayerId, uint posInList)
    {
        Debug.Log("Position in List: " + posInList);
        if (playerList[posInList].getPlayerId() == recPlayerId)
        {
            return true;
        } else
        {
            return false;
        }
    }

    private void addPlayerToList(int recConnectionId, uint newPlayerId)
    {
        //TODO: Erstmal nur 10 player erlauben
        if (playerCount == 10)
        {
            return;
        }

        uint positionToAddTo = nextSlot[playerCount];
        Debug.Log("Spieler wird geaddet auf: " + positionToAddTo);
        playerScript playerToAdd = ScriptableObject.CreateInstance<playerScript>();
        playerToAdd.constructNew(newPlayerId, recConnectionId, positionToAddTo, 0f, 0f, 0f);
        playerList[positionToAddTo] = playerToAdd;
        playerCount++;
        myNetwork.sendMessage("4;" + positionToAddTo + ";" + newPlayerId, recConnectionId);
        broadcastNewPlayer(positionToAddTo);
    }

	// Use this for initialization
	void Start () {
        Application.runInBackground = true;
        //TODO: Erstmal nur 10 player erlauben
        playerList = new playerScript[10];
        nextSlot = new uint[10];
        resetNextSlotList();
    }
	
    private void resetNextSlotList()
    {
        for (uint i = 0; i < nextSlot.GetLength(0); i++)
        {
            nextSlot[i] = i;
        }
    }

	// Update is called once per frame
	void Update () {
	
	}
}
