using UnityEngine;
using Verse;

namespace merissu
{
    public struct Offset
    {
        public Vector3 position;
        public float angle;
    }

    public struct DrawOffsetSet
    {
        public Offset northOffset;
        public Offset eastOffset;
        public Offset southOffset;
        public Offset westOffset;
    }

    public enum DrawPosition
    {
        None,
        Back,
        Side
    }
}