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

        // Start is called before the first frame update
        void Start()
        {
            scene = NetworkScene.FindNetworkScene(this);
            roomClient = scene.GetComponent<RoomClient>();
            avatarManager = scene.GetComponentInChildren<AvatarManager>();
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
                if (uiIndicator != null)// uiIndicator is of type NetworkedMainMenuIndicator and is the indicator of the local menu that would be sent to remote peers
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
    }

#if UNITY_EDITOR
    // add this code below the ClassWithVariablesYouWantToSetInEditor
    [CustomEditor(typeof(ExperimenterControls))]
    public class ExperimentControlsEditor : Editor
    {
        bool avatarHidden = false;

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