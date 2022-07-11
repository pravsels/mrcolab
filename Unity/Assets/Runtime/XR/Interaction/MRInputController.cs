using Microsoft.MixedReality.Toolkit.Input;
using UnityEngine;

namespace Ubiq.XR
{
    public class MRInputController : BaseInputHandler, IMixedRealityInputHandler
    {
        [HideInInspector]
        public bool inputUp = false;
        [HideInInspector]
        public bool inputDown = false;
        
        public void OnInputUp(InputEventData eventData)
        {
            inputUp = true;
            inputDown = false;
            Debug.Log(">>>>>>>>>>>>>> InputUp");
        }

        public void OnInputDown(InputEventData eventData)
        {
            inputDown = true;
            inputUp = false;
            Debug.Log(">>>>>>>>>>>>>> InputDown");
        }

        protected override void RegisterHandlers()
        {
        }

        protected override void UnregisterHandlers()
        {
        }
    }
}