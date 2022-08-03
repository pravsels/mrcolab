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
        public bool start;
        public Message(bool start)
        {
            this.start = start;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        context = NetworkScene.Register(this);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void StartScenario()
    {
        Timer.ResetTimer();
    }

    // Send start game message
    public void SendMessageUpdate()
    {
        Message startMessage = new Message();
        startMessage.start = true;
        context.SendJson(startMessage);
    }

    // Receive message that start the game
    public void ProcessMessage(ReferenceCountedSceneGraphMessage message)
    {
        var msg = message.FromJson<Message>();
        if (msg.start == true)
            StartScenario();
    }
}
