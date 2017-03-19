using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

public class networkScript : MonoBehaviour
{
    //Network
    private int connectionId;
    private int channelId;
    private int socketId;
    private int socketPort = 777;

    //Messages
    int recHostId;
    int recConnectionId;
    int recChannelId;
    byte[] recBuffer = new byte[1024];
    int bufferSize = 1024;
    int dataSize;
    string message;
    byte error;
    string[] splitMessage;

    NetworkEventType recNetworkEvent;
    Stream stream;
    BinaryFormatter formatter;

    //Game
    public gameScript myGame;

    private void openSocket()
    {
        //Oeffnet einen Socket der zum Senden und Empfangen verwendet wird
        NetworkTransport.Init();
        ConnectionConfig config = new ConnectionConfig();
        channelId = config.AddChannel(QosType.Reliable);

        int maxConnections = 10;
        HostTopology topology = new HostTopology(config, maxConnections);

        socketId = NetworkTransport.AddHost(topology, socketPort);
    }

    public void sendMessage(string message, int connectionId)
    {
        byte[] buffer = new byte[1024];
        stream = new MemoryStream(buffer);
        formatter = new BinaryFormatter();
        formatter.Serialize(stream, message);

        NetworkTransport.Send(socketId, connectionId, channelId, buffer, bufferSize, out error);
    }

    // Use this for initialization
    void Start()
    {
        openSocket();
    }

    private void checkMessages()
    {
        recNetworkEvent = NetworkTransport.Receive(out recHostId, out recConnectionId, out recChannelId, recBuffer, bufferSize, out dataSize, out error);

        if (error < 0)
        {
            Debug.Log("Error: " + error);
            return;
        }

        switch (recNetworkEvent)
        {
            case NetworkEventType.Nothing:
                break;
            case NetworkEventType.ConnectEvent:
                break;
            case NetworkEventType.DataEvent:
                stream = new MemoryStream(recBuffer);
                formatter = new BinaryFormatter();
                message = formatter.Deserialize(stream) as string;
                dissectMessage(recConnectionId, message);
                break;
            case NetworkEventType.DisconnectEvent:
                myGame.findLeaver(recConnectionId);
                break;
        }
    }

    private void dissectMessage(int recConnectionId, string recMessage)
    {
        //Nimmt die Nachricht auseinander und gibt sie an das Spiel
        splitMessage = recMessage.Split(';');
        myGame.processMessage(recConnectionId, splitMessage);
    }

    // Update is called once per frame
    void Update()
    {
        checkMessages();
    }
}
