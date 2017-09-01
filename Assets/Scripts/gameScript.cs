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
    CastPointStart = 5,
    CastTargetStart = 6,
    CastFreeStart = 7,
    CastPointEnd = 8,
    CastTargetEnd = 9,
    CastFreeEnd = 10,
    DealDamage = 11,
    DealHeal = 12,
    OnlineCheck = 13,
    DrainMana = 14,
    GiveMana = 15,
    ChangeTarget = 16,
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
    //TODO: private playerScript[] monsterList;
    private uint[] nextSlot;
    private string outMessage;
    private string recMessage;
    uint recPlayerPosInList;
    uint recTargetPosInList;
    uint recSpellPosInList;
    private Spell[] spellList;

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
                recMessage = message[3];
                sendChatMessage(recPlayer, recPlayerPosInList, recMessage);
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
            case Command.CastPointStart:
                //Jemand sagt uns, dass er einen AOE-Cast startet
                recPlayer = uint.Parse(message[1]);
                recPlayerPosInList = uint.Parse(message[2]);
                recX = float.Parse(message[3]);
                recY = float.Parse(message[4]);
                recZ = float.Parse(message[5]);
                recSpellPosInList = uint.Parse(message[6]);
                checkPointSpellCast(recPlayer, recPlayerPosInList, recX, recY, recZ, recSpellPosInList);
                break;
            case Command.CastTargetStart:
                //Jemand sagt uns, dass er einen Target-Cast startet
                recPlayer = uint.Parse(message[1]);
                recPlayerPosInList = uint.Parse(message[2]);
                recTargetPosInList = uint.Parse(message[3]);
                recSpellPosInList = uint.Parse(message[4]);
                checkTargetSpellCast(recPlayer, recPlayerPosInList, recTargetPosInList, recSpellPosInList);
                break;
            case Command.ChangeTarget:
                //Jemand aendert sein target
                recPlayer = uint.Parse(message[1]);
                recPlayerPosInList = uint.Parse(message[2]);
                recMessage = message[3];
                changePlayerTarget(recPlayer, recPlayerPosInList, recMessage);
                break;
            default:
                break;
        }
    }

    private void changePlayerTarget(uint recPlayer, uint recPlayerPosInList, string target)
    {
        if (checkIdentity(recPlayer, recPlayerPosInList))
        {
            if (target == "null")
            {
                playerList[recPlayerPosInList].setTarget(null);
                broadcastTargetChange(recPlayerPosInList, "null");
            } else
            {
                uint newTarget = uint.Parse(target);
                playerList[recPlayerPosInList].setTarget(playerList[newTarget]);
                broadcastTargetChange(recPlayerPosInList, target);
            }
        }
    }

    private void broadcastTargetChange(uint playerPosInList, string target)
    {
        for (uint i = 0; i < playerList.GetLength(0); i++)
        {
            if (playerList[i] == null || playerList[i] == playerList[playerPosInList])
            {
                continue;
            }
            outMessage = "16;" + playerPosInList + ";" + target;
            myNetwork.sendMessage(outMessage, playerList[i].getConnectionId());
        }
    }


    private void sendChatMessage(uint recPlayer, uint recPlayerPosInList, string recMessage)
    {
        if (checkIdentity(recPlayer, recPlayerPosInList))
        {
            for (uint i = 0; i < playerList.GetLength(0); i++)
            {
                if (playerList[i] == null)
                {
                    continue;
                }
                outMessage = "2;" + recPlayerPosInList + ";" + recMessage;
                myNetwork.sendMessage(outMessage, playerList[i].getConnectionId());
            }
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

    private void checkPointSpellCast(uint recPlayer, uint recPlayerPosInList, float recX, float recY, float recZ, uint spellID)
    {
        playerScript castingPlayer = playerList[recPlayerPosInList];
        //schauen ob er der ist, fuer den er sich ausgibt
        if (castingPlayer.getPlayerId() == recPlayer)
        {
            //Checken ob er genug Mana hat
            if (checkRessource(castingPlayer, spellList[spellID]))
            {
                //Cast geht durch
                broadcastPointSpellEnd(recPlayerPosInList, recX, recY, recZ, spellID);
            }
        }
    }

    private void checkTargetSpellCast(uint recPlayer, uint recPlayerPosInList, uint target, uint spellID)
    {
        playerScript castingPlayer = playerList[recPlayerPosInList];
        //schauen ob er der ist, fuer den er sich ausgibt
        if (castingPlayer.getPlayerId() == recPlayer)
        {
            //TODO: Schauen ob das Target valid ist
            //Checken ob er genug Mana hat
            if (checkRessource(castingPlayer, spellList[spellID]))
            {
                //Cast geht durch
                wipTargetCast(target, spellID);
                broadcastTargetSpellEnd(recPlayerPosInList, target, spellID);
            }
        }
    }

    //TODO: Das hier wird zu onCast() vom Spell
    private void wipTargetCast(uint target, uint spell)
    {
        playerScript spellTarget = playerList[target];
        Spell castedSpell = spellList[spell];
        switch (spell)
        {
            case 1:
                spellTarget.reduceRessource(false, castedSpell.spellDmg);
                break;
            case 2:
                spellTarget.increaseRessource(false, castedSpell.spellHeal);
                break;
            case 3:
                spellTarget.increaseRessource(true, castedSpell.spellHeal);
                break;
            default:
                break;
        }
    }

    private bool checkRessource(playerScript castingPlayer, Spell castedSpell)
    {
        int manaCost = castedSpell.spellManacost;
        int healthCost = castedSpell.spellHPcost;
        if (castingPlayer.getCurrentMana() >= manaCost && castingPlayer.getCurrentHealth() >= healthCost)
        {
            if (castedSpell.spellManacost > 0)
            {
                castingPlayer.reduceRessource(true, castedSpell.spellManacost);
            }
            if (castedSpell.spellHPcost > 0)
            {
                castingPlayer.reduceRessource(false, castedSpell.spellHPcost);
            }
            return true;
        }
        return false;
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
    private void broadcastPointSpellEnd(uint playerPosInList, float recX, float recY, float recZ, uint spellID)
    {
        for (uint i = 0; i < playerList.GetLength(0); i++)
        {
            if (playerList[i] == null)
            {
                continue;
            }
            outMessage = "8;" + playerPosInList + ";" + recX + ";" + (recY + 2f) + ";" + recZ + ";" + spellID;
            myNetwork.sendMessage(outMessage, playerList[i].getConnectionId());
        }
    }

    //TODO: Vorher noch den Spellbeginn durchgeben, damit die ihre Animationen und so anpassen
    //TODO: Evtl fuer alle Spells fit machen mit val1, val2 usw. und command anpassbar
    private void broadcastTargetSpellEnd(uint playerPosInList, uint target, uint spellID)
    {
        for (uint i = 0; i < playerList.GetLength(0); i++)
        {
            if (playerList[i] == null)
            {
                continue;
            }
            outMessage = "9;" + playerPosInList + ";" + target + ";" + spellID;
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
                //Debug.Log("Zeige Spieler " + i + " den neuen Spieler auf " + positionToAddTo);
                myNetwork.sendMessage(outMessage, playerList[i].getConnectionId());
                //Und dem Neuen diesen Spieler zeigen
                outMessage = "1;" + playerList[i].getListPos();
                //Debug.Log("Zeige neuem Spieler " + positionToAddTo + " den Spieler auf " + playerList[i].getListPos());
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
        //Debug.Log("Position in List: " + posInList);
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
        //Debug.Log("Spieler wird geaddet auf: " + positionToAddTo);
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

        //Spells
        spellList = new Spell[4];
        Spell newSpell = ScriptableObject.CreateInstance<Cube>();
        newSpell.initiate();
        spellList[0] = newSpell;
        newSpell = ScriptableObject.CreateInstance<Fireblast>();
        newSpell.initiate();
        spellList[1] = newSpell;
        newSpell = ScriptableObject.CreateInstance<Heal>();
        newSpell.initiate();
        spellList[2] = newSpell;
        newSpell = ScriptableObject.CreateInstance<Innervate>();
        newSpell.initiate();
        spellList[3] = newSpell;

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
