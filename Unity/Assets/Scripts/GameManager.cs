using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ubiq.Messaging;

public class GameManager : MonoBehaviour, INetworkComponent, INetworkObject
{
    // Networking components
    NetworkId INetworkObject.Id => new NetworkId("a13ba05dbb9ef8fc");
    private NetworkContext context;

    // Message sent to everyone to start the game
    struct Message
    {
        public int layer;     // nullable bool 
        public Message(int layer)
        {
            this.layer = layer;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        context = NetworkScene.Register(this);
    }

    public void SetBlocksLayer(int layer)
    {
        GameObject blocks = GameObject.Find("Manipulation");

        if (blocks != null)
        {
            BlocksHider hider = GetComponent<BlocksHider>();
            if (hider != null)
            {
                hider.SetLayer(layer);
            }
        }

        SendMessageUpdate(layer);
    }

    // Send start game message
    public void SendMessageUpdate(int layer)
    {
        Message msg = new Message(layer);
        context.SendJson(msg);
    }

    // Receive message that start the game
    public void ProcessMessage(ReferenceCountedSceneGraphMessage message)
    {
        var msg = message.FromJson<Message>();
        GameObject blocks = GameObject.Find("Manipulation");

        if (blocks != null)
        {
            BlocksHider hider = GetComponent<BlocksHider>();
            if (hider != null)
            {
                hider.SetLayer(msg.layer);
            }
        }
    }
}