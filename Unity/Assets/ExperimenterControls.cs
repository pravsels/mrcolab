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
        public ShelfLightController shelfLightController;
        private GameManager game_manager;

        // Start is called before the first frame update
        void Start()
        {
            scene = NetworkScene.FindNetworkScene(this);
            roomClient = scene.GetComponent<RoomClient>();
            avatarManager = scene.GetComponentInChildren<AvatarManager>();
            game_manager = GameObject.Find("Game Manager").GetComponent<GameManager>();
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

        }

        public void ResumeTimer()
        {

        }

        public void PauseTimer()
        {

        }

        public void SetBlocks(int layer)
        {
            game_manager.SetBlocksLayer(layer);
        }

        public void SetShelfLight(bool shelf_light)
        {
            if (shelfLightController != null)
                shelfLightController.setShelfLight(shelf_light);
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
        bool shelfLight = false;

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
                    Debug.Log("Hide avatar");
                }
            }

            if (GUILayout.Button(hideBlocks == false ? "Hide Blocks" : "Show Blocks"))
            {
                if (hideBlocks) // show blocks 
                {
                    hideBlocks = !hideBlocks;
                    t.SetBlocks(0);
                    Debug.Log("Show Blocks!");
                }
                else
                {
                    hideBlocks = !hideBlocks;
                    t.SetBlocks(8);
                    Debug.Log("Hide Blocks");
                }
            }

            if (GUILayout.Button("Start/Reset Scenario"))
            {
                t.ResetTimer();
                Debug.Log("Start Scenaiorio");
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


            if (GUILayout.Button("Show clue A"))
            {
                t.ShowPoster("SetA");
            }

            if (GUILayout.Button("Show clue B"))
            {
                t.ShowPoster("SetB");
            }

            if (GUILayout.Button("Show clue A"))
            {
                t.ShowPoster("SetA");
            }

            if (GUILayout.Button("Show clue B"))
            {
                t.ShowPoster("SetB");
            }

            if (GUILayout.Button("Show clue A Text"))
            {
                t.ShowPoster("SetAText");
            }

            if (GUILayout.Button("Show clue B Text"))
            {
                t.ShowPoster("SetBText");
            }

            if (GUILayout.Button(shelfLight == false ? "Shelf On" : "Shelf Off"))
            {
                if (shelfLight) // show blocks 
                {
                    shelfLight = !shelfLight;
                    t.SetShelfLight(shelfLight);
                    Debug.Log("Switch-off Shelf Light!");
                }
                else
                {
                    shelfLight = !shelfLight;
                    t.SetShelfLight(shelfLight);
                    Debug.Log("Switch On Shelf Light");
                }
            }
        }
    }
#endif
}