using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace Cards.Adapters
{
    /// <summary>
    /// Drives a hand interactor's Select from the hand joints: fingers curled
    /// into a fist = grab = Select. Needed where the runtime delivers joints but
    /// no pinch/grasp device (XR Interaction Simulator's Hand mode, PICO over PC
    /// streaming). Assign the hand's Near-Far Interactor in the Inspector.
    /// </summary>
    public class JointGrabSelect : MonoBehaviour
    {
        [Tooltip("The hand's interactor whose Select this drives. Drag the hand's Near-Far Interactor here.")]
        [SerializeField] private XRBaseInputInteractor interactor;

        [SerializeField] private Handedness handedness = Handedness.Right;

        [Tooltip("Average fingertip-to-palm distance, in metres, at which a grab starts.")]
        [SerializeField] private float grabDistance = 0.05f;

        [Tooltip("Distance at which the grab ends. Larger than Grab Distance.")]
        [SerializeField] private float releaseDistance = 0.07f;

        private static readonly List<XRHandSubsystem> subsystems = new List<XRHandSubsystem>();

        // The 4 fingertips used to measure the grab (the thumb is left out).
        private static readonly XRHandJointID[] fingerTips =
        {
            XRHandJointID.IndexTip, XRHandJointID.MiddleTip,
            XRHandJointID.RingTip, XRHandJointID.LittleTip
        };

        private GrabDetector detector;
        private XRHandSubsystem hands;
        private XRInputButtonReader.InputSourceMode originalMode;
        private bool inControl;

        private void Awake() => detector = new GrabDetector(grabDistance, releaseDistance);

        private void OnDisable() => HandBack();

        private void Update()
        {
            if (interactor == null) return;

            TakeOver();

            if (!TryGetCurl(out float distance))
            {
                // Hand lost: let go rather than keep holding with stale data.
                detector.Reset();
                interactor.selectInput.QueueManualState(false, 0f);
                return;
            }

            bool grabbing = detector.Update(distance);
            interactor.selectInput.QueueManualState(grabbing, detector.Strength(distance));
        }

        /// <summary>Takes Select over from its input action, which never fires here.</summary>
        private void TakeOver()
        {
            if (inControl) return;

            originalMode = interactor.selectInput.inputSourceMode;
            interactor.selectInput.inputSourceMode = XRInputButtonReader.InputSourceMode.ManualValue;
            inControl = true;
        }

        /// <summary>Gives Select back to its original input source.</summary>
        private void HandBack()
        {
            if (!inControl || interactor == null) return;

            interactor.selectInput.QueueManualState(false, 0f);
            interactor.selectInput.inputSourceMode = originalMode;
            inControl = false;
            detector?.Reset();
        }

        /// <summary>Average fingertip-to-palm distance: small = fist, large = open hand.</summary>
        private bool TryGetCurl(out float distance)
        {
            distance = 0f;

            if (hands == null || !hands.running)
            {
                hands = FindRunningSubsystem();
                if (hands == null) return false;
            }

            XRHand hand = handedness == Handedness.Left ? hands.leftHand : hands.rightHand;
            if (!hand.isTracked) return false;

            if (!hand.GetJoint(XRHandJointID.Palm).TryGetPose(out Pose palm)) return false;

            float total = 0f;
            foreach (XRHandJointID tipId in fingerTips)
            {
                if (!hand.GetJoint(tipId).TryGetPose(out Pose tip)) return false;
                total += Vector3.Distance(tip.position, palm.position);
            }

            distance = total / fingerTips.Length;
            return true;
        }

        private static XRHandSubsystem FindRunningSubsystem()
        {
            SubsystemManager.GetSubsystems(subsystems);

            foreach (XRHandSubsystem subsystem in subsystems)
                if (subsystem.running) return subsystem;

            return null;
        }
    }
}
