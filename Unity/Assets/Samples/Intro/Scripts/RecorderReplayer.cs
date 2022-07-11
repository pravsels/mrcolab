﻿using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
using UnityEngine;
using UnityEditor;
using Ubiq.Messaging;
using Ubiq.Networking;
using Ubiq.Avatars;
using Avatar = Ubiq.Avatars.Avatar;
using Ubiq.Spawning;
using Ubiq.Rooms;
using Ubiq.Samples;
using RecorderReplayerTypes;
using Ubiq.Voip;

public class Recorder
{
    // Recording
    public event EventHandler OnRecordingStopped;

    private RecorderReplayer recRep;

    private BinaryWriter binaryWriter;
    private string recordFileIDs; // save the objectIDs of the recorded avatars

    private int frameNr = 0;
    private int previousFrame = 0;
    private Dictionary<NetworkId, string> textures; // textures
    private Dictionary<NetworkId, string> recordedObjectIds; // ids and prefabs   
    private List<float> frameTimes = new List<float>(); 
    private List<int> pckgSizePerFrame;
    private List<long> idxFrameStart; // start of a specific frame in the recorded data, entry in list could be a huge value
    private bool initFile = false;
    private MessagePack messages = null;
   
    private float recordingStartTime = 0.0f;

    public Recorder(RecorderReplayer recRep)
    {
        this.recRep = recRep;
        textures = new Dictionary<NetworkId, string>();
        recordedObjectIds = new Dictionary<NetworkId, string>();
        pckgSizePerFrame = new List<int>();
        idxFrameStart = new List<long>();
    }

    public int GetFrameNr() { return frameNr; }

    // so we know how many of the messages belonge to one frame,
    // this is called after all connections have received their messages after one Update()
    public void NextFrame()
    {
        previousFrame = frameNr;
        frameNr += 1;
    }

    public bool IsRecording()
    {
        Debug.Log("Recording...");
        return recRep.recording;
    }

    // prepare recording file
    // data structure for file 
    public void RecordMessage(INetworkObject obj, ReferenceCountedSceneGraphMessage message)
    {
        if (!initFile)
        {
            //var dateTime = System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            recRep.recordFile = recRep.path + "/rec" + recRep.recordingStartTimeString + ".dat";
            recordFileIDs = recRep.path + "/IDsrec" + recRep.recordingStartTimeString + ".txt";
            recordedObjectIds = new Dictionary<NetworkId, string>();

            idxFrameStart.Add(0); // first frame in byte data has idx 0

            binaryWriter = new BinaryWriter(File.Open(recRep.recordFile, FileMode.OpenOrCreate)); // dispose when recording is finished
            // start recoding time
            recordingStartTime = Time.unscaledTime;

            // create message pack for first frame (frameNr == 0)
            messages = new MessagePack();

            initFile = true;
        }

        //Debug.Log("Framenr: " + frameNr + " " + previousFrame);
        if (previousFrame != frameNr) // went on to next frame so generate new message pack
        {
            if (messages != null)
            {
                byte[] bMessages = messages.GetBytes();
                binaryWriter.Write(bMessages);
                pckgSizePerFrame.Add(bMessages.Length);
                long idx = idxFrameStart[idxFrameStart.Count - 1] + bMessages.Length;
                idxFrameStart.Add(idx);
                frameTimes.Add(Time.unscaledTime - recordingStartTime);
                //Debug.Log("FrameNr, pckgsize, idxFrameStart " + frameNr + " " + pckgSizePerFrame.Count + " " + idxFrameStart.Count);
            }
            messages = new MessagePack();
            previousFrame += 1; 
        }

        var msg = new byte[message.length + 10]; // just take header and message
        Buffer.BlockCopy(message.bytes, 0, msg, 0, message.length + 10);
        SingleMessage recMsg = new SingleMessage(msg);
        byte[] recBytes = recMsg.GetBytes();
          
        messages.AddMessage(recBytes);
        //Debug.Log(message.objectid.ToString() + " " + frameNr);

        if (!recordedObjectIds.ContainsKey(message.objectid)) // dictionary ContainsKey is O(1) and List Contains is O(n)
        {
            if (obj is Avatar) // check it here too in case we later record other things than avatars as well
            {
                var name = (obj as Avatar).PrefabUuid;
                string uid = (obj as Avatar).gameObject.GetComponent<TexturedAvatar>().GetTextureUuid(); // get texture of avatar so we can later replay a look-alike avatar
                recordedObjectIds.Add(message.objectid, name); // prefab
                if (!textures.ContainsKey(message.objectid))
                {
                    textures.Add(message.objectid, uid);
                }
            }
            else
            {
                string name = (obj as MonoBehaviour).name;
                string uid = "n";
                // not every object might have a TexturedObject component (e.g. the menu)
                if ((obj as MonoBehaviour).gameObject.TryGetComponent<TexturedObject>(out TexturedObject texturedObject))
                {
                    uid = texturedObject.GetTextureUid();
                }
                textures.Add(message.objectid, uid); 
                recordedObjectIds.Add(message.objectid, name); 
            }
        }

    }

    public void SaveRecordingInfo()
    {
        if (recordedObjectIds != null && recordFileIDs != null) // save objectids and texture uids once recording is done
        {
            Debug.Log("Save recording info");
            binaryWriter.Dispose();

            Debug.Log("FrameNr, pckgsize, idxFrameStart" + frameNr + " " + pckgSizePerFrame.Count + " " + idxFrameStart.Count);

            recRep.audioRecRep.WriteLastSamplesOnRecordingStopped();
            var audioInfoData = recRep.audioRecRep.GetAudioRecInfoData(); // order of objectids could be different than order in recordedObjectIds (only has avatar ids)

            File.WriteAllText(recordFileIDs, JsonUtility.ToJson(new RecordingInfo(frameNr-1, 
                new List<NetworkId>(audioInfoData.Item1.Keys), new List<short>(audioInfoData.Item1.Values), new List<int>(audioInfoData.Item2),
                recordedObjectIds.Count,
                new List<NetworkId>(recordedObjectIds.Keys), new List<string>(textures.Values), new List<string>(recordedObjectIds.Values),
                frameTimes, pckgSizePerFrame, idxFrameStart), true));

            // Clear variables
            OnRecordingStopped.Invoke(this, EventArgs.Empty);
            textures.Clear();
            recordedObjectIds.Clear();
            recordFileIDs = null;
            pckgSizePerFrame.Clear();
            idxFrameStart.Clear();
            frameTimes.Clear();
            recordingStartTime = 0.0f;
            messages = null;

            initFile = false;
            frameNr = previousFrame = 0;
            Debug.Log("Recording info saved");

        }
    }
}

[System.Serializable]
public class Replayer
{
    // Replaying
    RecorderReplayer recRep;

    public event EventHandler<RecordingInfo> OnLoadingReplay;
    public event EventHandler OnReplayRepeat;
    public event EventHandler<bool> OnReplayPaused;
    public event EventHandler OnReplayStopped;

    private NetworkSpawner spawner;

    private ReferenceCountedSceneGraphMessage[][] replayedMessages;
    private int[] replayedFrames;
    public RecordingInfo recInfo = null;
    private Dictionary<NetworkId, string> prefabs; // joined lists from recInfo
    private Dictionary<NetworkId, string> textures; // joined lists from recInfo
    // later for the recording of other objects consider not only saving the networkid but additional info such as class
    // maybe save info in Dictionary list and save objectid (key) and values (list: class, (if avatar what avatar type + texture info)
    private Dictionary<NetworkId, string> replayedObjectids; // avatar IDs and texture
    public Dictionary<NetworkId, ReplayedObjectProperties> replayedObjects; // new objectids for all other objects! 
    public Dictionary<NetworkId, NetworkId> oldNewIds;
    private bool loadingStarted = false; // set to true once loading recorded data starts
    private bool loaded = false;
    private FileStream streamFromFile;
    private bool objectsCreated = false;
    private bool opened = false;
 
    public Replayer(RecorderReplayer recRep)
    {
        this.recRep = recRep;

        replayedObjectids = new Dictionary<NetworkId, string>();
        replayedObjects = new Dictionary<NetworkId, ReplayedObjectProperties>();
        oldNewIds = new Dictionary<NetworkId, NetworkId>();
        spawner = recRep.spawner;
        recRep.menuRecRep.OnPointerUp += MenuRecRep_OnPointerUp;
    }

    public void Replay(string replayFile)
    {
        if (!loadingStarted)
        {
            //Debug.Log("1 " + recRep.play);
            LoadRecording(replayFile);
            //Debug.Log("2 " + recRep.play);

        }

        if (recRep.play)
        {
            if (loaded) // meaning recInfo
            {
                recRep.replayingStartTime += Time.deltaTime;
                
                var replayTime = recInfo.frameTimes[recRep.currentReplayFrame];

                while (replayTime < recRep.replayingStartTime) // catch up with currentReplayFrame to current time t
                {
                    //Debug.Log("Catch up: " + replayTime + " " + recRep.replayingStartTime + " " + recRep.currentReplayFrame);
                    ReplayFromFile();
                    UpdateFrame();
                    replayTime = recInfo.frameTimes[recRep.currentReplayFrame];
                }

                if (recRep.currentReplayFrame > 0)
                {
                    var prev = recInfo.frameTimes[recRep.currentReplayFrame - 1];
                    // if current replayTime is closer to current frame time replay current frame too, otherwise continue
                    if (Math.Abs(replayTime - recRep.replayingStartTime) < Math.Abs(prev - recRep.replayingStartTime))
                    {
                        ReplayFromFile(); // replay current frame
                        UpdateFrame(); // update to next frame
                    }
                }
                else
                {
                    ReplayFromFile();
                    UpdateFrame();
                }
            }
        }
        //else // !play 
        //{
        //    // TODO: consider cleaning up the complete recording whenever jumping to a frame.
        //    if (loaded)
        //    {
        //        //Debug.Log("!play");
        //        if(recRep.currentReplayFrame < recRep.sliderFrame)
        //        {
        //            //Debug.Log("after");
        //            while (recRep.currentReplayFrame < recRep.sliderFrame)
        //            {
        //                ReplayFromFile();
        //                recRep.currentReplayFrame++;
        //            }
        //        }
        //        else if(recRep.currentReplayFrame > recRep.sliderFrame)
        //        {
        //            HideAll();
        //            recRep.currentReplayFrame = 0;
        //            streamFromFile.Position = 0;
        //            while (recRep.currentReplayFrame < recRep.sliderFrame)
        //            {
        //                ReplayFromFile();
        //                recRep.currentReplayFrame++;
        //            }
        //        }
        //        Debug.Log(recRep.currentReplayFrame + " " + recRep.sliderFrame);
        //        recRep.currentReplayFrame = recRep.sliderFrame;
        //        recRep.stopTime = recInfo.frameTimes[recRep.currentReplayFrame];
        //        recRep.replayingStartTime = recInfo.frameTimes[recRep.currentReplayFrame];
        //        ReplayFromFile();
        //        // jump to correct position in audio clips
        //        recRep.audioRecRep.JumpToFrame(recRep.currentReplayFrame, recInfo.frames);
                
        //    }
        //}
    }

    private void MenuRecRep_OnPointerUp(object sender, EventArgs e)
    {
        // TODO: consider cleaning up the complete recording whenever jumping to a frame.
        if (loaded)
        {
            //Debug.Log("!play");
            if (recRep.currentReplayFrame < recRep.sliderFrame)
            {
                //Debug.Log("after");
                while (recRep.currentReplayFrame < recRep.sliderFrame)
                {
                    ReplayFromFile();
                    recRep.currentReplayFrame++;
                }
            }
            else if (recRep.currentReplayFrame > recRep.sliderFrame)
            {
                HideAll();
                recRep.currentReplayFrame = 0;
                streamFromFile.Position = 0;
                while (recRep.currentReplayFrame < recRep.sliderFrame)
                {
                    ReplayFromFile();
                    recRep.currentReplayFrame++;
                }
            }
            Debug.Log("Jump to frame: " + recRep.currentReplayFrame + " " + recRep.sliderFrame);
            recRep.currentReplayFrame = recRep.sliderFrame;
            recRep.stopTime = recInfo.frameTimes[recRep.currentReplayFrame];
            recRep.replayingStartTime = recInfo.frameTimes[recRep.currentReplayFrame];
            ReplayFromFile();
            // jump to correct position in audio clips
            recRep.audioRecRep.JumpToFrame(recRep.currentReplayFrame, recInfo.frames);

        }
    }

    private void UpdateFrame()
    {
        recRep.currentReplayFrame++;
        if (recRep.currentReplayFrame == recInfo.frames)
        {
            OnReplayRepeat.Invoke(this, EventArgs.Empty);
            recRep.currentReplayFrame = 0;
            HideAll();
            streamFromFile.Position = 0;
            recRep.replayingStartTime = 0.0f;
            recRep.stopTime = 0.0f;
            //Debug.Log("Reset frame");
        }
        recRep.sliderFrame = recRep.currentReplayFrame;
    }

    private bool CreateRecordedObjects()
    {
        foreach (var item in prefabs)
        {
            var objectid = item.Key;
            var prefabName = item.Value;
            var uid = textures[objectid]; // value is "n" if there is no texture
            GameObject prefab = spawner.catalogue.GetPrefab(prefabName);
            if (prefab == null)
            {
                Debug.Log("Continue: " + objectid.ToString() + " " + prefabName);
                continue;
            }
            GameObject go = spawner.SpawnPersistentReplay(prefab, false, uid, true, new TransformMessage(recRep.thisTransform));
            
            ReplayedObjectProperties props = new ReplayedObjectProperties();
            if (go.TryGetComponent(out ObjectHider objectHider))
            {
                //props.hider = go.GetComponent<ObjectHider>();
                props.hider = objectHider;
            }
            Debug.Log("CreateRecordedObjects():  " + go.name);
            NetworkId newId = go.GetComponent<INetworkObject>().Id;
            oldNewIds.Add(objectid, newId);
            Debug.Log(objectid.ToString() + " new: " + newId.ToString());
            props.gameObject = go;
            props.id = newId;
            // Nels' magic leap room threw an error because the posters he had in the room as child game objects cannot be added because the key already exists
            INetworkComponent[] components = go.GetComponentsInChildren<INetworkComponent>(); 
            foreach (var comp in components)
            {
                props.components.Add(NetworkScene.GetComponentId(comp), comp);
            }
            replayedObjects.Add(newId, props);

        }
        return true;
    }

    public async void LoadRecording(string replayFile)
    {
        loadingStarted = true;

        string filepath = recRep.path + "/IDs" + replayFile + ".txt";
        if (File.Exists(filepath))
        {
            Debug.Log("Load info...");
            recInfo = await LoadRecInfo(filepath);
            OnLoadingReplay.Invoke(this, recInfo);

            Debug.Log(recInfo.frames + " " + recInfo.frameTimes.Count + " " + recInfo.pckgSizePerFrame.Count);
            objectsCreated = CreateRecordedObjects();
            recRep.audioRecRep.OnLoadingReplay(recInfo);
            Debug.Log("Info loaded!");
        }
        else
        {
            Debug.Log("Invalid replay file ID path!");
            recRep.replaying = false;
            loadingStarted = false;
            
        }

        filepath = recRep.path + "/" + replayFile + ".dat";
        if (File.Exists(filepath))
        {
            opened = OpenStream(filepath);
        }
        else
        {
            Debug.Log("Invalid replay file plath!");
            recRep.replaying = false;
            loadingStarted = false;
        }
        loaded = objectsCreated && opened;
    }
    private bool OpenStream(string filepath)
    {
        streamFromFile = File.Open(filepath, FileMode.Open); // dispose once replaying is done
        recRep.replayingStartTime = 0.0f;
        return true;
    }

    private async Task<RecordingInfo> LoadRecInfo(string filepath)
    {
        RecordingInfo recInfo;
        using (StreamReader reader = File.OpenText(filepath))
        {
            string recString = await reader.ReadToEndAsync();

            recInfo = JsonUtility.FromJson<RecordingInfo>(recString);
            prefabs = recInfo.objectids.Zip(recInfo.prefabs, (k, v) => new { k, v }).ToDictionary(x => x.k, x => x.v);
            textures = recInfo.objectids.Zip(recInfo.textures, (k, v) => new { k, v }).ToDictionary(x => x.k, x => x.v);
        }

        return recInfo;
    }

    private ReferenceCountedSceneGraphMessage[] CreateRCSGMs(MessagePack messagePack)
    {
        ReferenceCountedSceneGraphMessage[] rcsgms = new ReferenceCountedSceneGraphMessage[messagePack.messages.Count-1]; // first index is size
        for (int i = 1; i < messagePack.messages.Count; i++) // start at index 1 bc 0 is size
        {
            byte[] msg = messagePack.messages[i];
            rcsgms[i - 1] = CreateRCSGM(msg);
        }
        return rcsgms;
    }
    // wouldn't it be nice to be able to skip a recorded message if something went wrong, so we just don't replay it
    private ReferenceCountedSceneGraphMessage CreateRCSGM(byte[] msg)
    {
        ReferenceCountedMessage rcm = new ReferenceCountedMessage(msg);
        ReferenceCountedSceneGraphMessage rcsgm = new ReferenceCountedSceneGraphMessage(rcm);
        NetworkId id = new NetworkId(msg, 0);
        try
        {
            rcsgm.objectid = oldNewIds[id];
        }
        catch (Exception e)
        {
            if (e.Source != null)
                Debug.Log("Network id: " + id + ", Component " + rcsgm.componentid);
            foreach(var i in oldNewIds)
            {
                Debug.Log("old: " + i.Key + " new: " + i.Value);
            }
            throw;
        }    
        return rcsgm;
    }

    
    private void ReplayFromFile()
    {
        var pckgSize = recInfo.pckgSizePerFrame[recRep.currentReplayFrame];
        streamFromFile.Position = recInfo.idxFrameStart[recRep.currentReplayFrame];
        byte[] msgPack = new byte[pckgSize];

        var numberBytes = streamFromFile.Read(msgPack, 0, pckgSize);

        int i = 4; // first 4 bytes are length of package
        while (i < numberBytes)
        {
            int lengthMsg = BitConverter.ToInt32(msgPack, i);
            i += 4;
            byte[] msg = new byte[lengthMsg];
            Buffer.BlockCopy(msgPack, i, msg, 0, lengthMsg);

            ReferenceCountedSceneGraphMessage rcsgm = CreateRCSGM(msg);
            //Debug.Log(rcsgm.objectid.ToString());
            ReplayedObjectProperties props = replayedObjects[rcsgm.objectid]; // avatars and objects
            //Debug.Log(rcsgm.componentid);
            INetworkComponent component = props.components[rcsgm.componentid];

            // send and replay remotely
            recRep.scene.Send(rcsgm);
            // replay locally
            component.ProcessMessage(rcsgm);

            i += lengthMsg;
        }
    }

    private void HideAll()
    {
        foreach (ReplayedObjectProperties props in replayedObjects.Values)
        {
           if (props.hider)
            {
                props.hider.NetworkedHide();
            }
        }
    }

    public void Cleanup(bool unspawn)
    {
        OnReplayStopped.Invoke(this, EventArgs.Empty);
        Debug.Log("Cleanup " + Time.unscaledTime);
        foreach (var i in oldNewIds)
        {
            Debug.Log("Cleanup ids old: " + i.Key + " new: " + i.Value);
        }

        loadingStarted = loaded = objectsCreated = false;
        recRep.play = false;
        recRep.currentReplayFrame = 0;
        recRep.sliderFrame = 0;
        // only unspawn while in room, NOT when leaving the room as it will be unspawned by the OnLeftRoom event anyways.
        //if (unspawn && replayedAvatars.Count > 0)
        //{
        //    foreach (var ids in replayedAvatars.Keys)
        //    {
        //        spawner.UnspawnPersistent(ids);
        //    }
        //}
        if (unspawn && replayedObjects.Count > 0)
        {
            foreach (var ids in replayedObjects.Keys)
            {
                spawner.UnspawnPersistent(ids);
            }
        }
        replayedObjects.Clear();
        oldNewIds.Clear();
        recInfo = null;
        if (prefabs != null)
        {
            prefabs.Clear();
            textures.Clear();
        }
        
        if (streamFromFile != null)
            streamFromFile.Close();
    }
}
[RequireComponent(typeof(AudioRecorderReplayer))]
public class RecorderReplayer : MonoBehaviour, IMessageRecorder, INetworkComponent
{
    public NetworkScene scene;
    public AudioRecorderReplayer audioRecRep;
    public RecorderReplayerMenu menuRecRep;
    [HideInInspector] public AvatarManager aManager;
    [HideInInspector] public NetworkSpawner spawner;
    private bool Recording = false; // this variable indicates if a recording is taking place, this doesn't need to be the local recording!
    private NetworkContext context;

    public string replayFile;
    [HideInInspector] public string recordFile = null;
    [HideInInspector] public string audioRecordFile = null;
    [HideInInspector] public string path;
    [HideInInspector] public bool recording, replaying;
    [HideInInspector] public bool play = false;
    [HideInInspector] public int sliderFrame = 0;
    [HideInInspector] public float stopTime = 0.0f;
    [HideInInspector] public float replayingStartTime = 0.0f;
    [HideInInspector] public bool loop = true;
    [HideInInspector] public int currentReplayFrame = 0;
    [HideInInspector] public bool reverse = false;
    [HideInInspector] public Transform thisTransform;

    [HideInInspector] public bool leftRoom = false;
    private RoomClient roomClient;
    [HideInInspector] public Recorder recorder;
    [HideInInspector] public Replayer replayer;
    [HideInInspector] public bool recordingAvailable = false;
    [HideInInspector] public bool cleanedUp = true;

    [HideInInspector] public string recordingStartTimeString;

    public void SetRecordingStartTime(string recordingStartTimeString)
    {
        this.recordingStartTimeString = recordingStartTimeString;
    }

    public struct Message
    {
        public bool recording;

        public Message(bool recording) { this.recording = recording; }
    }

    public void ProcessMessage(ReferenceCountedSceneGraphMessage message)
    {
        Message msg = message.FromJson<Message>();
        Recording = msg.recording;
        Debug.Log("Yeahh is there a recording happening? " + Recording);
    }

    public bool IsOwner()
    {
        return roomClient.Me["creator"] == "1";
    }

    void Awake()
    {
        //Application.targetFrameRate = 60;
        //Time.captureFramerate = 400;
        path = Application.persistentDataPath + "/Recordings";

        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
        aManager = scene.GetComponentInChildren<AvatarManager>();
        spawner = NetworkSpawner.FindNetworkSpawner(scene);

        // create Recorder and Replayer
        Debug.Log("Assign RecorderReplayer to Recorder and Replayer");
        recorder = new Recorder(this);
        replayer = new Replayer(this);
        play = false; // I am not entirely sure why it is true otherwise
    }

    public struct RoomMessage
    {
        public string peerUuid;
        public bool isRecording;
    }

    void Start ()
    {
        thisTransform = gameObject.transform;
        context = scene.RegisterComponent(this);
        roomClient = scene.GetComponent<RoomClient>();
        //roomClient.OnPeerRemoved.AddListener(OnPeerRemoved);
        roomClient.OnJoinedRoom.AddListener(OnJoinedRoom);
        roomClient.OnRoomUpdated.AddListener(OnRoomUpdated);
        roomClient.OnPeerUpdated.AddListener(OnPeerUpdated);
        roomClient.Me["creator"] = "1"; // so recording is also enabled when not being in a room at startup
        roomClient.Room["Recorder"] = JsonUtility.ToJson(new RoomMessage() { peerUuid = roomClient.Me.UUID, isRecording = Recording });
    }
    public void OnJoinedRoom(IRoom room)
    {
        roomClient.Me["creator"] = "0";
        #if UNITY_EDITOR
        roomClient.Me["creator"] = "1";
        #endif
        Debug.Log("OnJoinedRoom RecorderReplayer");
        // if (roomClient.Peers.Count() == 0)
        // {
        //     Debug.Log("No peers in room, make me creator");
        //     roomClient.Me["creator"] = "1";
        // }
        // else if (roomClient.Peers.Count() == 1) // this one has to be the creator so if they leave let me become creator
        // {
        //     Debug.Log("Creator in the room, let me become successor");
        //     roomClient.Me["creator"] = "2";
        // }
        // else
        // {
        //     Debug.Log(roomClient.Peers.Count() + "other peer(s) in the room, someone else is creator");
        //     roomClient.Me["creator"] = "0";
        // }
    }

    // if previous authority gets discoonected make sure that recording is stopped when authority is given to the next peer
    // it shouldnt even be necessary to call it here
    public void OnPeerUpdated(IPeer peer)
    {
        // just so i dont forget what i was thinking here...
        if (peer["creator"] == "1") // if creator just got reassigned in the room client this could be true
        {
            // need to make sure that if this is called because of another update and this peer had the authority anyways, that we do not 
            // end a recording that should not be ended... 
            if (!recording) // this is true if the authority just got reassigned because then the new peer never did a recording before
            {
                // so we can safely globally set the Recording to false for everyone in case the previous peer who got disconnected cannot do this anymore
                Debug.Log("RecorderReplayer: OnPeerUdated");
                Recording = false; // should be false already because of StopRecording() in RoomClient!
                // update the properties in the room for the new recorder authority...
                roomClient.Room["Recorder"] = JsonUtility.ToJson(new RoomMessage() { peerUuid = roomClient.Me.UUID, isRecording = Recording });
            }
        }
    }

    public void OnRoomUpdated(IRoom room)
    {
        if (roomClient.Me["creator"] == "1")
        {
            //Debug.Log("I record: " + Recording + " " + roomClient.Me.UUID);
            roomClient.Room["Recorder"] = JsonUtility.ToJson(new RoomMessage() { peerUuid = roomClient.Me.UUID, isRecording = Recording });
        }
        else
        //if (roomClient.Me["creator"] != "1") 
        {
            if (room["Recorder"] != null)
            {
                RoomMessage msg = JsonUtility.FromJson<RoomMessage>(room["Recorder"]);
                Recording = msg.isRecording;
                //Debug.Log("Someone records: " + Recording);
            }
            else
            {
                Debug.Log("No recorder was added to the dictionary... this shouldn't be.");
            }
        }
    }

    //public void OnPeerRemoved(IPeer peer)
    //{
    //    if (peer["creator"] == "1") // this might be called when the removed peer who was the creator isn't even the creator anymore...
    //    {
    //        Debug.Log("RecRep: OnPeerRemoved");
    //        cleanedUp = true; 
    //        replayer.Cleanup(true);
    //        Recording = false;
    //        roomClient.Room["Recorder"] = JsonUtility.ToJson(new RoomMessage() { peerUuid = roomClient.Me.UUID, isRecording = Recording });

    //        if (replaying)
    //        {
    //            replaying = false;
    //            replayingStartTime = 0.0f;
    //            stopTime = 0.0f;
    //            Debug.Log("Left room, replaying stopped!");
    //        }
    //        if (recording)
    //        {
    //            recording = false;
    //            Debug.Log("Left room, recording stopped!");
    //        }
    //    }
    //}

    private void OnDestroy()
    {
        Debug.Log("OnDestroy");
        //replayer.Cleanup(true); objects should be removed by each client when OnPeerRemoved is called
        if (recording)
        {
            Recording = recording = false;
            // this probably isn't sent anymore as the NetworkScene got already destroyed
            roomClient.Room["Recorder"] = JsonUtility.ToJson(new RoomMessage() { peerUuid = roomClient.Me.UUID, isRecording = Recording });

            recorder.SaveRecordingInfo();
        }
    }

    // Update is called once per frame
    void Update()
    {
       if (roomClient.Me["creator"] == "1") // don't bother if we are not room creators
        {
            //Debug.Log(play);

            if (!recording)
            {
                if (Recording)
                {
                    Recording = false;
                    roomClient.Room["Recorder"] = JsonUtility.ToJson(new RoomMessage() { peerUuid = roomClient.Me.UUID, isRecording = Recording });
                    Debug.Log("Tell everyone we STOPPED recording");
                }

                if (recordingAvailable)
                {
                    recorder.SaveRecordingInfo();

                    // stop replaying once recording stops as it does not make sense to see the old replay since there is already a new one
                    if (replaying)
                    {
                        replaying = false;
                        cleanedUp = true;
                        replayer.Cleanup(true);
                        replayingStartTime = 0.0f;
                        stopTime = 0.0f;
                    }

                    //SetReplayFile();
                    recordingAvailable = false; // avoid unnecessary savings of same info (is checked in methods too)
                }
            }
           else
            {
                if (!Recording)
                {
                    Recording = true;
                    roomClient.Room["Recorder"] = JsonUtility.ToJson(new RoomMessage() { peerUuid = roomClient.Me.UUID, isRecording = Recording });
                    Debug.Log("Tell everyone we ARE recording");
                }
                recordingAvailable = true;
            }

            if (replaying)
            {
                replayer.Replay(replayFile);
                cleanedUp = false;
            }
            else
            {
                if (!cleanedUp)
                {
                    cleanedUp = true;
                    replayer.Cleanup(true);
                    replayingStartTime = 0.0f;
                    stopTime = 0.0f;
                }
            }
        }
    }
    public void SetReplayFile()
    {
        // sets the previously recorded file as replay file
        replayFile = Path.GetFileNameWithoutExtension(recordFile); 
        Debug.Log("Set replay file to " + replayFile);
        recordFile = null;
    }

    public void RecordMessage(INetworkObject networkObject, ReferenceCountedSceneGraphMessage message)
    {
        recorder.RecordMessage(networkObject, message);
    }

    public void NextFrame()
    {
        recorder.NextFrame();
    }

    public void StopRecording()
    {
        Recording = false;
    }

    // returns if a recording is going on, no matter if we are recording or someone else is
    public bool IsRecording()
    {
        return Recording;
    }

}
//# if UNITY_EDITOR
//[CustomEditor(typeof(RecorderReplayer))]
//public class RecorderReplayerEditor : Editor
//{

//    public override void OnInspectorGUI()
//    {
//        var t = (RecorderReplayer)target;
//        DrawDefaultInspector();

//        if (Application.isPlaying)
//        {
//            EditorGUI.BeginDisabledGroup(!t.IsOwner());
//            if (GUILayout.Button(t.recording == true ? "Stop Recording" : "Record"))
//            {
//                t.recording = !t.recording;
//            }
//            t.replaying = EditorGUILayout.Toggle("Replaying", t.replaying);
//            if (t.replaying)
//            {
//                //t.cleanedUp = false;
//                if (GUILayout.Button(t.play == true ? "Stop" : "Play"))
//                {
//                    if (!t.play)
//                    {
//                        //t.replayingStartTime = Time.unscaledTime;
//                        t.replayingStartTime = t.replayer.recInfo.frameTimes[t.currentReplayFrame];
//                    }
//                    t.play = !t.play;
//                }
//                if (!t.play)
//                {
//                    t.sliderFrame = EditorGUILayout.IntSlider(t.sliderFrame, 0, t.replayer.recInfo.frames);
//                }
//            }
//            EditorGUI.EndDisabledGroup();
//        }
//    }
//}
//# endif

