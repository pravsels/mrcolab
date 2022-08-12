using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Ubiq.Avatars;
using Ubiq.Messaging;
using Avatar = Ubiq.Avatars.Avatar;
using Ubiq.Rooms;
using Ubiq.Samples;

namespace Ubiq.Samples
{
    public class ExperimenterControls : MonoBehaviour
    {
        private AvatarManager avatarManager;
        private NetworkScene scene;
        private RoomClient roomClient;
        private ObjectHider avatarHider;
        private ObjectHider menuHider;
        private NetworkedMainMenuIndicator uiIndicator;
        public SocialMenu socialMenu;
        public PosterController posterController;
        private GameManager game_manager;

        // Start is called before the first frame update
        void Start()
        {
            scene = NetworkScene.FindNetworkScene(this);
            roomClient = scene.GetComponent<RoomClient>();
            avatarManager = scene.GetComponentInChildren<AvatarManager>();
            game_manager = GameObject.Find("Game Manager").GetComponent<GameManager>();
        }

        // Update is called once per frame
        void Update()
        {

        }

        public void ShowHideAvatar(int layer)
        {
            if (avatarManager.LocalAvatar != null)
            {
                avatarManager.LocalAvatar.Peer["visible"] = layer == 0 ? "1" : "0";
                // if not in a room use this
                avatarHider = avatarManager.LocalAvatar.gameObject.GetComponent<ObjectHider>();
                avatarHider.SetLayer(layer);
                if (uiIndicator != null) // uiIndicator is of type NetworkedMainMenuIndicator and is the indicator of the local menu that would be sent to remote peers
                {
                    // menuHider is the ObjectHider from the menu indicator
                    menuHider.SetNetworkedObjectLayer(layer);
                }
            }
        }

        public void ShowPoster(string setName)
        {
            posterController.setPosterVisibility(setName);
        }

        public void ResetTimer()
        {
            game_manager.SendMessageUpdate(true, null, null);    // start and resume 
            game_manager.StartScenario();
            Timer.paused = false;
        }

        public void ResumeTimer()
        {
            game_manager.SendMessageUpdate(null, false, null);   // start and resume
            game_manager.ResumeTimer();
        }

        public void PauseTimer()
        {
            game_manager.SendMessageUpdate(null, true, null);   // start and pause
            game_manager.PauseTimer();
        }

        public void HideBlocks()
        {
            game_manager.SendMessageUpdate(null, null, true);
            game_manager.SetLayerOfBlocks(8);
        }

        public void ShowBlocks()
        {
            game_manager.SendMessageUpdate(null, null, false);
            game_manager.SetLayerOfBlocks(0);
        }
    }

#if UNITY_EDITOR
    // add this code below the ClassWithVariablesYouWantToSetInEditor
    [CustomEditor(typeof(ExperimenterControls))]
    public class ExperimentControlsEditor : Editor
    {
        bool avatarHidden = false;
        bool pauseTimer = false;
        bool hideBlocks = false; 

        public override void OnInspectorGUI()
        {
            var t = (ExperimenterControls)target;
            DrawDefaultInspector();
            if (GUILayout.Button(avatarHidden == true ? "Show Avatar" : "Hide Avatar"))
            {
                if (avatarHidden) // make avatar visible again
                {
                    avatarHidden = !avatarHidden;
                    t.ShowHideAvatar(0);
                    Debug.Log("Show avatar");
                }
                else
                {
                    avatarHidden = !avatarHidden;
                    t.ShowHideAvatar(8);
                    Debug.Log(" Hide avatar");
                }
            }

            if (GUILayout.Button(hideBlocks == false ? "Hide Blocks" : "Show Blocks"))
            {
                if (hideBlocks == false) // show blocks 
                {
                    hideBlocks = !hideBlocks;
                    t.ShowBlocks();
                    Debug.Log("Show Blocks!");
                }
                else
                {
                    hideBlocks = !hideBlocks;
                    t.HideBlocks();
                    Debug.Log("Hide Blocks");
                }
            }

            if (GUILayout.Button(pauseTimer == true ? "Resume Timer" : "Pause Timer"))
            {
                if (pauseTimer) // resume timer
                {
                    pauseTimer = !pauseTimer;
                    t.ResumeTimer();
                    Debug.Log("Resume Timer");
                }
                else
                {
                    pauseTimer = !pauseTimer;
                    t.PauseTimer();
                    Debug.Log("Pause Timer");
                }
            }

            if (GUILayout.Button("Start/Reset Scenario"))
            {
                t.ResetTimer();
                Debug.Log("Start Scenaior");
            }

            if (GUILayout.Button("Show poster set A"))
            {
                t.ShowPoster("SetA");
            }

            if (GUILayout.Button("Show poster set B"))
            {
                t.ShowPoster("SetB");
            }
        }
    }
#endif
}