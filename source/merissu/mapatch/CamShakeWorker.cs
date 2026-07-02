using AM.Events;
using AM.Events.Workers;
using UnityEngine;
using Verse;

namespace merissu.Events
{
    public class CamShakeWorker : EventWorkerBase
    {
        public override string EventID => "CamShakeEvent";

        public override void Run(AnimEventInput i)
        {
            var e = i.Event as CamShakeEvent;
            if (e == null) return;

            Vector3 animPos = i.Animator.RootTransform.MultiplyPoint3x4(Vector3.zero);
            Map map = i.Animator.Map;

            Vector3 camPos = Find.Camera.transform.position;
            float dist = Vector3.Distance(camPos, animPos);
            if (dist > e.MaxDistance) return;

            float factor = Mathf.Clamp01(1f - dist / e.MaxDistance);
            float intensity = e.Magnitude * factor;

            Find.CameraDriver.shaker.DoShake(intensity);
        }
    }
}
