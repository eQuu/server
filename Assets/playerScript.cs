using UnityEngine;
using System.Collections;

public class playerScript : ScriptableObject {

    //TODO: bei der Id nochmal ueberlegen was ingameId ist und was characterId ist und was accountId ist und so weiter
    private uint playerId;
    private int connectionId;
    private uint listPosition;
    private float posX, posY, posZ;
    private float rotY;
    private float movespeed;
    private int currentMana, currentHealth;
    private int maximumMana, maximumHealth;
    private playerScript target = null;

    //TODO: Erstmal ohne Rotation
    public void constructNew(uint newId,int recConnectionId, uint positionInList, float newX, float newY, float newZ)
    {
        this.playerId = newId;
        this.connectionId = recConnectionId;
        this.listPosition = positionInList;
        this.posX = newX;
        this.posY = newY;
        this.posZ = newZ;
        this.rotY = 0f;
        this.movespeed = 1.0f;
        this.maximumMana = 100;
        this.currentMana = maximumMana;
    }

	// Use this for initialization
	void Start () {

    }
	
	// Update is called once per frame
	void Update () {
	
	}

    public void setTarget(playerScript newTarget)
    {
        this.target = newTarget;
    }

    public uint getPlayerId()
    {
        return this.playerId;
    }

    public int getConnectionId()
    {
        return this.connectionId;
    }

    public uint getListPos()
    {
        return this.listPosition;
    }

    public int getCurrentMana()
    {
        return this.currentMana;
    }

    public void reduceRessource(bool isMana, int amount)
    {
        if (isMana)
        {
            this.currentMana -= amount;
            if (currentMana < 0)
            {
                currentMana = 0;
            }
        } else
        {
            this.currentHealth -= amount;
            if (currentHealth < 0)
            {
                currentHealth = 0;
            }
        }
    }

    public void setPosition(float x, float y, float z)
    {
        this.posX = x;
        this.posY = y;
        this.posZ = z;
    }

    public void setRotation(float y)
    {
        this.rotY = y;
    }
}
