using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ubiq.Avatars;
using Ubiq.Samples;

namespace Ubiq.Avatars
{
    [RequireComponent(typeof(Avatar))]
    [RequireComponent(typeof(FloatingAvatar))]
    [RequireComponent(typeof(ThreePointTrackedAvatar))]
    public class ScaleAvatar : MonoBehaviour
    {

        public enum World
        {
            HugeWorld,
            NormalWorld,
            SmallWorld
        }

        public static World localWorld = World.NormalWorld;

        private Transform sameTransform;
        private Transform normalTransform;
        private Transform smallTransform;
        private Transform hugeTransform;

        private Avatar avatar;
        private FloatingAvatar floatingAvatar;
        private ThreePointTrackedAvatar threePointTrackedAvatar;

        private void Awake()
        {
            avatar = GetComponent<Avatar>();
            floatingAvatar = GetComponent<FloatingAvatar>();
            threePointTrackedAvatar = GetComponent<ThreePointTrackedAvatar>();
        }

        // Start is called before the first frame update
        void Start()
        {
            
        }

        // Update is called once per frame
        void Update()
        {

        }
    }
}