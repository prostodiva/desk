using System;
using System.Collections;
using Cards.Core;
using UnityEngine;

namespace Cards.Presentation
{
    /// <summary>
    /// Moves a card between its slot in the stack and the inspect anchor.
    ///
    /// Parenting to the anchor at zero local position/rotation is deliberate:
    /// the card inherits the anchor's orientation exactly, so the pose is
    /// whatever you set up visually in the editor, with no angle maths that
    /// can be wrong.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class CardMotion : MonoBehaviour, ICardMotion
    {
        [SerializeField] private float travelDuration = 0.35f;
        [SerializeField] private float flipDuration = 0.3f;

        private Rigidbody rb;
        private Transform homeParent;
        private Vector3 homeLocalPosition;
        private Quaternion homeLocalRotation;
        private Coroutine routine;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            CaptureHome();
        }

        public void CaptureHome()
        {
            homeParent = transform.parent;
            homeLocalPosition = transform.localPosition;
            homeLocalRotation = transform.localRotation;
        }

        public void MoveToAnchor(Transform anchor, Action onComplete = null)
        {
            if (anchor == null) return;
            Restart(ToAnchorRoutine(anchor, onComplete));
        }

        public void MoveHome(Action onComplete = null)
        {
            Restart(HomeRoutine(onComplete));
        }

        public void Flip()
        {
            // Interrupting a flight would drop its onComplete, which is what
            // makes the card selectable again.
            if (routine != null) return;
            routine = StartCoroutine(FlipRoutine());
        }

        private void Restart(IEnumerator next)
        {
            if (routine != null) StopCoroutine(routine);
            routine = StartCoroutine(next);
        }

        private IEnumerator ToAnchorRoutine(Transform anchor, Action onComplete)
        {
            Freeze();

            Vector3 startPos = transform.position;
            Quaternion startRot = transform.rotation;
            transform.SetParent(anchor, worldPositionStays: true);

            // Interpolate in the anchor's local space so head movement
            // mid-flight doesn't throw the card off course.
            Vector3 localStart = anchor.InverseTransformPoint(startPos);
            Quaternion localStartRot = Quaternion.Inverse(anchor.rotation) * startRot;

            float t = 0f;
            while (t < travelDuration)
            {
                t += Time.deltaTime;
                float eased = Mathf.SmoothStep(0f, 1f, t / travelDuration);
                transform.localPosition = Vector3.Lerp(localStart, Vector3.zero, eased);
                transform.localRotation = Quaternion.Slerp(localStartRot, Quaternion.identity, eased);
                yield return null;
            }

            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;

            routine = null;
            onComplete?.Invoke();
        }

        private IEnumerator HomeRoutine(Action onComplete)
        {
            Freeze();

            Vector3 startPos = transform.position;
            Quaternion startRot = transform.rotation;
            //Put it back under CardStack
            transform.SetParent(homeParent, worldPositionStays: true);
            //Work out the target in world space
            Vector3 targetPos = homeParent != null
                ? homeParent.TransformPoint(homeLocalPosition)
                : homeLocalPosition;
            Quaternion targetRot = homeParent != null
                ? homeParent.rotation * homeLocalRotation
                : homeLocalRotation;

            float t = 0f;
            //Fly a little each frame
            while (t < travelDuration)
            {
                t += Time.deltaTime;
                float eased = Mathf.SmoothStep(0f, 1f, t / travelDuration);
                transform.position = Vector3.Lerp(startPos, targetPos, eased);
                transform.rotation = Quaternion.Slerp(startRot, targetRot, eased);
                yield return null;
            }

            //Snap exactly into the slot
            transform.localPosition = homeLocalPosition;
            transform.localRotation = homeLocalRotation;

            //"not moving any more", Flip may run again
            routine = null;
            //SetInteractable(true): the card can be used again
            onComplete?.Invoke();
        }

        /// <summary>
        /// Half-turn around the card's long axis (local Z), like turning a
        /// page. Going home restores the home rotation, so a flipped card
        /// always lands face up.
        /// </summary>
        private IEnumerator FlipRoutine()
        {
            Quaternion start = transform.localRotation;

            float t = 0f;
            while (t < flipDuration)
            {
                t += Time.deltaTime;
                float eased = Mathf.SmoothStep(0f, 1f, t / flipDuration);
                transform.localRotation = start * Quaternion.AngleAxis(180f * eased, Vector3.forward);
                yield return null;
            }

            transform.localRotation = start * Quaternion.AngleAxis(180f, Vector3.forward);
            routine = null;
        }

        private void Freeze()
        {
            if (rb == null) return;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }
    }
}
