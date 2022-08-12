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
        public bool? start, paused, hide_blocks;     // nullable bool 
        public Message(bool? start, bool? paused, bool? hide_blocks)
        {
            this.start = start;
            this.paused = paused;
            this.hide_blocks = hide_blocks;
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

    public void SetLayerOfBlocks(int layer)
    {
        GameObject world_blocks = GameObject.FindGameObjectWithTag("Manipulation");
        if (world_blocks != null)
        {
            ObjectHider blocksHider = world_blocks.GetComponent<ObjectHider>();
            if (blocksHider != null)
            {
                blocksHider.SetLayer(layer);
            }
        }
    }

    // Send start game message
    public void SendMessageUpdate(bool? start, bool? paused, bool? hide_blocks)
    {
        Message msg = new Message(start, paused, hide_blocks);
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
        else if (msg.paused == false)
            ResumeTimer();

        if (msg.hide_blocks == true)
        {
            SetLayerOfBlocks(8);    
        } else if (msg.hide_blocks == false)
        {
            SetLayerOfBlocks(0);
        }
    }
}