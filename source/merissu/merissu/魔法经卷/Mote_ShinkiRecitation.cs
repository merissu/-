using UnityEngine;
using Verse;

namespace merissu
{
    public class Mote_ShinkiRecitation : Mote
    {
        public int currentFrame = 0;
        private int frameCounter = 0;
        private const int TicksPerFrame = 3;    
        private const int MaxFrame = 5;
        private bool forward = true;
        private bool readyToDestroy = false;    

        public Vector3 offset = Vector3.zero;

        protected override void Tick()
        {
            if (readyToDestroy)
            {
                this.Destroy();
                return;
            }

            base.Tick();
            if (this.Destroyed) return;

            if (this.link1.Linked)
            {
                this.exactPosition = this.link1.Target.Thing.DrawPos + offset;
            }
            frameCounter++;
            if (frameCounter >= TicksPerFrame)
            {
                frameCounter = 0;

                if (forward)
                {
                    currentFrame++;
                    if (currentFrame >= MaxFrame)
                    {
                        currentFrame = MaxFrame;
                        forward = false;
                    }
                }
                else
                {
                    currentFrame--;
                    if (currentFrame <= 0)
                    {
                        currentFrame = 0;
                        readyToDestroy = true;
                        return;
                    }
                }
            }
        }
    }
}