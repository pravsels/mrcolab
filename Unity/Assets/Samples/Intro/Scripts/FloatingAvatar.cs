using Ubiq.Avatars;
using UnityEngine;
using UnityEngine.Networking;
using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Text;

namespace Ubiq.Samples
{
    /// <summary>
    /// Recroom/rayman style avatar with hands, torso and head
    /// </summary>
    [RequireComponent(typeof(Avatars.Avatar))]
    [RequireComponent(typeof(ThreePointTrackedAvatar))]
    public class FloatingAvatar : MonoBehaviour
    {
        public Transform head;
        public Transform torso;
        public Transform leftHand;
        public Transform rightHand;

        public Renderer headRenderer;
        public Renderer torsoRenderer;
        public Renderer leftHandRenderer;
        public Renderer rightHandRenderer;

        public Transform baseOfNeckHint;

        // public float torsoFacingHandsWeight;
        public AnimationCurve torsoFootCurve;

        public AnimationCurve torsoFacingCurve;

        public TexturedAvatar texturedAvatar;

        private Avatars.Avatar avatar;
        private ThreePointTrackedAvatar trackedAvatar;
        private Vector3 footPosition;
        private Quaternion torsoFacing;

        private Vector3 scaleChange;
        private bool change_scale_min;
        private bool change_scale_max;
        private bool change_size_normal;
        private bool is_abnormal_size;
        private GameObject player;

        private HttpListener listener = null;
        private Thread listener_thread;
        private float time_spent_abnormal;
        private float abnormal_time_ = 10f;
        private float avatar_min_size = 0.1f;
        private float avatar_max_size = 3f;

        private void Start()
        {
            change_scale_min = false;
            change_scale_max = false;
            change_size_normal = false;
            is_abnormal_size = false;
            time_spent_abnormal = abnormal_time_;

            if (listener == null && SystemInfo.deviceModel.ToLower().Contains("quest"))
            {
                try
                {
                    // set up HTTP listener on port 4444
                    listener = new HttpListener();
                    listener.Prefixes.Add("http://*:4444/");
                    listener.AuthenticationSchemes = AuthenticationSchemes.Anonymous;
                    listener.Start();
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                }

                listener_thread = new Thread(startListener);
                listener_thread.Start();
            }
        }

        private void Awake()
        {
            avatar = GetComponent<Avatars.Avatar>();
            trackedAvatar = GetComponent<ThreePointTrackedAvatar>();
            player = GameObject.FindGameObjectWithTag("Player");
        }

        private void startListener()
        {
            while (true)
            {
                var result = listener.BeginGetContext(ListenerCallback, listener);
                result.AsyncWaitHandle.WaitOne();
            }
        }

        private void ListenerCallback(IAsyncResult result)
        {
            var context = listener.EndGetContext(result);

            Debug.Log("Method: " + context.Request.HttpMethod);
            Debug.Log("LocalUrl: " + context.Request.Url.LocalPath);

            if (context.Request.Url.LocalPath == "/minimize")
            {
                change_scale_min = true; 
            } else if (context.Request.Url.LocalPath == "/maximize")
            {
                change_scale_max = true; 
            }
            context.Response.Close();
        }

        private void OnEnable()
        {
            trackedAvatar.OnHeadUpdate.AddListener(ThreePointTrackedAvatar_OnHeadUpdate);
            trackedAvatar.OnLeftHandUpdate.AddListener(ThreePointTrackedAvatar_OnLeftHandUpdate);
            trackedAvatar.OnRightHandUpdate.AddListener(ThreePointTrackedAvatar_OnRightHandUpdate);

            if (texturedAvatar)
            {
                texturedAvatar.OnTextureChanged.AddListener(TexturedAvatar_OnTextureChanged);
            }

            scaleChange = new Vector3(0.01f, 0.01f, 0.01f);
        }

        private void OnDisable()
        {
            if (trackedAvatar && trackedAvatar != null)
            {
                trackedAvatar.OnHeadUpdate.RemoveListener(ThreePointTrackedAvatar_OnHeadUpdate);
                trackedAvatar.OnLeftHandUpdate.RemoveListener(ThreePointTrackedAvatar_OnLeftHandUpdate);
                trackedAvatar.OnRightHandUpdate.RemoveListener(ThreePointTrackedAvatar_OnRightHandUpdate);
            }

            if (texturedAvatar && texturedAvatar != null)
            {
                texturedAvatar.OnTextureChanged.RemoveListener(TexturedAvatar_OnTextureChanged);
            }
        }

        private void ThreePointTrackedAvatar_OnHeadUpdate(Vector3 pos, Quaternion rot)
        {
            head.position = pos;
            head.rotation = rot;
        }

        private void ThreePointTrackedAvatar_OnLeftHandUpdate(Vector3 pos, Quaternion rot)
        {
            leftHand.position = pos;
            leftHand.rotation = rot;
        }

        private void ThreePointTrackedAvatar_OnRightHandUpdate(Vector3 pos, Quaternion rot)
        {
            rightHand.position = pos;
            rightHand.rotation = rot;
        }

        private void TexturedAvatar_OnTextureChanged(Texture2D tex)
        {
            headRenderer.material.mainTexture = tex;
            torsoRenderer.material = headRenderer.material;
            leftHandRenderer.material = headRenderer.material;
            rightHandRenderer.material = headRenderer.material;
        }

        private void Update()
        {
            UpdateTorso();

            UpdateVisibility();

            if (change_scale_min || change_scale_max)
            {
                int multiplier = change_scale_max == true ? 1 : -1;

                head.transform.localScale += multiplier*scaleChange;
                torso.transform.localScale += multiplier*scaleChange;
                leftHand.transform.localScale += multiplier*scaleChange;
                rightHand.transform.localScale += multiplier*scaleChange;
                player.transform.localScale += multiplier*scaleChange;
            }

            if (head.transform.localScale.y < avatar_min_size)
            {
                change_scale_min = false;
            }
            if (head.transform.localScale.y > avatar_max_size)
            {
                change_scale_max = false;
            }
        }

        private void FixedUpdate()
        {
            if (change_scale_max || change_scale_min)
            {
                is_abnormal_size = true; 
            }

            if (is_abnormal_size)
            {
                time_spent_abnormal -= Time.fixedDeltaTime;

                if (time_spent_abnormal < 0f)
                {
                    is_abnormal_size = false;
                    time_spent_abnormal = abnormal_time_;
                    change_size_normal = true;
                }
            }

            if (change_size_normal)
            {
                float size_diff = 1f - head.transform.localScale.y;
                int multiplier = size_diff < 0f ? -1 : 1;
                head.transform.localScale += multiplier * scaleChange;
                torso.transform.localScale += multiplier * scaleChange;
                leftHand.transform.localScale += multiplier * scaleChange;
                rightHand.transform.localScale += multiplier * scaleChange;
                player.transform.localScale += multiplier * scaleChange;
            }

            if (change_size_normal)
            {
                if (Math.Abs(1f - head.transform.localScale.y) <= 0.02f)
                {
                    change_size_normal = false; 
                }
            }
        }

        private void UpdateVisibility()
        {
            if (avatar.IsLocal)
            {
                //if(renderToggle != null && renderToggle.rendering)
                {
                    headRenderer.enabled = false;
                    torsoRenderer.enabled = true;
                    leftHandRenderer.enabled = true;
                    rightHandRenderer.enabled = true;
                }
                //else
                //{
                //    headRenderer.enabled = false;
                //    torsoRenderer.enabled = false;
                //    leftHandRenderer.enabled = false;
                //    rightHandRenderer.enabled = false;
                //}

                //renderToggle.Send();
            }
            else
            {
                //if (renderToggle != null && renderToggle.rendering)
                {
                    headRenderer.enabled = true;
                    torsoRenderer.enabled = true;
                    leftHandRenderer.enabled = true;
                    rightHandRenderer.enabled = true;

                }
                //else
                //{
                //    headRenderer.enabled = false;
                //    torsoRenderer.enabled = false;
                //    leftHandRenderer.enabled = false;
                //    rightHandRenderer.enabled = false;
                //}
            }
            //renderToggle.Send();

        }

        private void UpdateTorso()
        {
            // Give torso a bit of dynamic movement to make it expressive

            // Update virtual 'foot' position, just for animation, wildly inaccurate :)
            var neckPosition = baseOfNeckHint.position;
            footPosition.x += (neckPosition.x - footPosition.x) * Time.deltaTime * torsoFootCurve.Evaluate(Mathf.Abs(neckPosition.x - footPosition.x));
            footPosition.z += (neckPosition.z - footPosition.z) * Time.deltaTime * torsoFootCurve.Evaluate(Mathf.Abs(neckPosition.z - footPosition.z));
            footPosition.y = 0;

            // Forward direction of torso is vector in the transverse plane
            // Determined by head direction primarily, hint provided by hands
            var torsoRotation = Quaternion.identity;

            // Head: Just use head direction
            var headFwd = head.forward;
            headFwd.y = 0;

            // Hands: TODO (this breaks too much currently)
            // Hands: Imagine line between hands, take normal (in transverse plane)
            // Use head orientation as a hint to give us which normal to use
            // var handsLine = rightHand.position - leftHand.position;
            // var handsFwd = new Vector3(-handsLine.z,0,handsLine.x);
            // if (Vector3.Dot(handsFwd,headFwd) < 0)
            // {
            //     handsFwd = new Vector3(handsLine.z,0,-handsLine.x);
            // }
            // handsFwdStore = handsFwd;

            // var headRot = Quaternion.LookRotation(headFwd,Vector3.up);
            // var handsRot = Quaternion.LookRotation(handsFwd,Vector3.up);

            // // Rotation is handsRotation capped to a distance from headRotation
            // var headToHandsAngle = Quaternion.Angle(headRot,handsRot);
            // Debug.Log(headToHandsAngle);
            // var rot = Quaternion.RotateTowards(headRot,handsRot,Mathf.Clamp(headToHandsAngle,-torsoFacingHandsWeight,torsoFacingHandsWeight));

            // // var rot = Quaternion.SlerpUnclamped(handsRot,headRot,torsoFacingHeadToHandsWeightRatio);

            var rot = Quaternion.LookRotation(headFwd, Vector3.up);
            var angle = Quaternion.Angle(torsoFacing, rot);
            var rotateAngle = Mathf.Clamp(Time.deltaTime * torsoFacingCurve.Evaluate(Mathf.Abs(angle)), 0, angle);
            torsoFacing = Quaternion.RotateTowards(torsoFacing, rot, rotateAngle);

            // Place torso so it makes a straight line between neck and feet
            torso.position = neckPosition;
            torso.rotation = Quaternion.FromToRotation(Vector3.down, footPosition - neckPosition) * torsoFacing;
        }

        // private Vector3 handsFwdStore;

        // private void OnDrawGizmos()
        // {
        //     Gizmos.color = Color.blue;
        //     Gizmos.DrawLine(head.position, footPosition);
        //     // Gizmos.DrawLine(head.position,head.position + handsFwdStore);
        // }
    }
}