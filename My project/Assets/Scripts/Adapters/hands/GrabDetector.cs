namespace Cards.Adapters
{
    /// <summary>
    /// Decides "grabbing or not" from how curled the fingers are (the average
    /// fingertip-to-palm distance). Two thresholds: a grab starts below one and
    /// ends only above the other, so a held grab doesn't flicker. Plain C#, so
    /// it can be unit-tested.
    /// </summary>
    public sealed class GrabDetector
    {
        private readonly float grabDistance;
        private readonly float releaseDistance;

        public GrabDetector(float grabDistance, float releaseDistance)
        {
            this.grabDistance = grabDistance;
            this.releaseDistance = releaseDistance;
        }

        public bool IsGrabbing { get; private set; }

        /// <summary>Feeds the latest distance; returns whether the hand is grabbing.</summary>
        public bool Update(float distance)
        {
            IsGrabbing = IsGrabbing ? distance < releaseDistance : distance < grabDistance;
            return IsGrabbing;
        }

        /// <summary>0 when open (at release distance or more), 1 when fully closed.</summary>
        public float Strength(float distance)
        {
            if (releaseDistance <= grabDistance) return IsGrabbing ? 1f : 0f;

            float t = (releaseDistance - distance) / (releaseDistance - grabDistance);
            return t < 0f ? 0f : t > 1f ? 1f : t;
        }

        public void Reset() => IsGrabbing = false;
    }
}
