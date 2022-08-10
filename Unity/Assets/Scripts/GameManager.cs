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
        public bool start, paused;
        public Message(bool start, bool paused)
        {
            this.start = start;
            this.paused = paused;
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

    public void PauseTimer()
    {
        Timer.paused = true;
    }

    public void ResumeTimer()
    {
        Timer.paused = false; 
    }

    // Send start game message
    public void SendMessageUpdate(bool start, bool paused)
    {
        Message msg = new Message();
        msg.start = start;
        msg.paused = paused; 
        context.SendJson(msg);
    }

    // Receive message that start the game
    public void ProcessMessage(ReferenceCountedSceneGraphMessage message)
    {
        var msg = message.FromJson<Message>();
        if (msg.start == true)
            StartScenario();
        if (msg.paused == true)
            PauseTimer();
        if (msg.paused == false)
            ResumeTimer();
    }
}
