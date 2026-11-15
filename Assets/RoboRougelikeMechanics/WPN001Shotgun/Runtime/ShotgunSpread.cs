using System;
using UnityEngine;

namespace RoboRougelike.WPN001Shotgun
{
    // Deterministic area-stratified pellet pattern inside a CONE OF FULL ANGLE.
    // Does not mutate UnityEngine.Random state. Not cryptographic; do not use as network authority.
    public static class ShotgunSpread
    {
        private const float GoldenAngleRadians = 2.3999632297286533f;

        public readonly struct IndexedPattern
        {
            private readonly Vector3 axis;
            private readonly Quaternion basis;
            private readonly int count;
            private readonly float offset,cosEdge;
            private readonly bool direct;
            public IndexedPattern(Vector3 forward,int count,float fullConeDegrees,uint seed)
            {
                if(count<1)throw new ArgumentOutOfRangeException(nameof(count));
                if(!IsFinite(forward.x)||!IsFinite(forward.y)||!IsFinite(forward.z)||forward.sqrMagnitude<1e-10f)
                    throw new ArgumentException("forward must be a finite, non-zero vector",nameof(forward));
                if(!IsFinite(fullConeDegrees))throw new ArgumentException("cone must be finite",nameof(fullConeDegrees));
                this.count=count;axis=forward.normalized;
                float halfRadians=Mathf.Clamp(fullConeDegrees,0f,90f)*Mathf.Deg2Rad*.5f;
                direct=count==1||halfRadians<=0f;
                basis=Quaternion.FromToRotation(Vector3.forward,axis);
                uint x=seed==0u?0x6D2B79F5u:seed;
                x^=x<<13;x^=x>>17;x^=x<<5;
                offset=(x&0x00FFFFFFu)*(Mathf.PI*2f/16777216f);
                cosEdge=Mathf.Cos(halfRadians);
            }
            public Vector3 DirectionAt(int index)
            {
                if(index<0||index>=count)throw new ArgumentOutOfRangeException(nameof(index));
                if(direct)return axis;
                // Preserve the original float arithmetic for existing salvos;
                // large indices use double so the angle does not lose precision.
                float u=count<=64?(index+.5f)/count:(float)((index+.5d)/count);
                float cosTheta=1f-u*(1f-cosEdge);
                float sinTheta=Mathf.Sqrt(Mathf.Max(0f,1f-cosTheta*cosTheta));
                float x,y;
                if(count<=64)
                {
                    float azimuth=offset+GoldenAngleRadians*index;
                    x=Mathf.Cos(azimuth)*sinTheta;y=Mathf.Sin(azimuth)*sinTheta;
                }
                else
                {
                    double azimuth=offset+2.3999632297286533d*index;
                    x=(float)Math.Cos(azimuth)*sinTheta;y=(float)Math.Sin(azimuth)*sinTheta;
                }
                return (basis*new Vector3(x,y,cosTheta)).normalized;
            }
        }

        public static uint VolleySeed(uint baseSeed, uint successfulShotIndex)
        {
            // Different successful salvos receive independent patterns.
            uint x = baseSeed ^ (successfulShotIndex * 0x9E3779B9u) ^ 0xA341316Cu;
            x ^= x >> 16; x *= 0x7FEB352Du;
            x ^= x >> 15; x *= 0x846CA68Bu;
            return x ^ (x >> 16);
        }

        public static int BuildDirections(Vector3 forward, int count, float fullConeDegrees,
            uint seed, Vector3[] output)
        {
            if (output == null) throw new ArgumentNullException(nameof(output));
            if (count < 1 || output.Length < count)
                throw new ArgumentOutOfRangeException(nameof(count), "count must be positive and fit output");
            if (!IsFinite(forward.x) || !IsFinite(forward.y) || !IsFinite(forward.z)
                || forward.sqrMagnitude < 1e-10f)
                throw new ArgumentException("forward must be a finite, non-zero vector", nameof(forward));
            if (!IsFinite(fullConeDegrees)) throw new ArgumentException("cone must be finite", nameof(fullConeDegrees));
            if(count>64)
            {
                // Use the same stable indexed pattern as the actual shotgun
                // view; the caller owns and sizes the output buffer.
                var pattern=new IndexedPattern(forward,count,fullConeDegrees,seed);
                for(int i=0;i<count;i++)output[i]=pattern.DirectionAt(i);
                return count;
            }
            float halfRadians = Mathf.Clamp(fullConeDegrees, 0f, 90f) * Mathf.Deg2Rad * 0.5f;
            Vector3 axis = forward.normalized;
            if (count == 1 || halfRadians <= 0f)
            {
                for (int i = 0; i < count; i++) output[i] = axis;
                return count;
            }

            Quaternion basis = Quaternion.FromToRotation(Vector3.forward, axis);
            uint x = seed == 0u ? 0x6D2B79F5u : seed;
            x ^= x << 13; x ^= x >> 17; x ^= x << 5;
            float offset = (x & 0x00FFFFFFu) * (Mathf.PI * 2f / 16777216f);
            float cosEdge = Mathf.Cos(halfRadians);

            for (int i = 0; i < count; i++)
            {
                // Equal-area annuli, one sample per annulus, golden-angle azimuth.
                float u = (i + 0.5f) / count;
                float cosTheta = 1f - u * (1f - cosEdge);
                float sinTheta = Mathf.Sqrt(Mathf.Max(0f, 1f - cosTheta * cosTheta));
                float azimuth = offset + GoldenAngleRadians * i;
                Vector3 local = new Vector3(Mathf.Cos(azimuth) * sinTheta,
                    Mathf.Sin(azimuth) * sinTheta, cosTheta);
                output[i] = (basis * local).normalized;
            }
            return count;
        }

        private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
