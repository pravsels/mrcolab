using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ubiq.XR;
using Ubiq.Messaging;

namespace Ubiq.Samples
{
    public class NetworkedSpace : MonoBehaviour, INetworkObject, INetworkComponent
    {
        public bool owner = false;
        public NetworkId Id { get; set; }
        private NetworkContext context;
        private Renderer m_Renderer;

        public struct Message
        {
            public TransformMessage transform;

            public Message(Transform transform)
            {
                this.transform = new TransformMessage(transform.position, transform.rotation);
            }
        }

        public void ProcessMessage(ReferenceCountedSceneGraphMessage message)
        {
            var msg = message.FromJson<Message>();
            transform.position = msg.transform.position; // The Message constructor will take the *local* properties of the passed transform.
            transform.rotation = msg.transform.rotation;
        }

        // Start is called before the first frame update
        void Start()
        {
            context = NetworkScene.Register(this);
            // this.m_Renderer = GetComponent<Renderer>();

            #if !UNITY_EDITOR && !UNITY_ANDROID // We assume this means we're on the HoloLens
            // this.m_Renderer.enabled = false;
            owner = true;
            #endif

        }

        // Update is called once per frame
        void Update()
        {
            if (owner)
            {
                context.SendJson(new Message(transform));
            }
        }
    }
}