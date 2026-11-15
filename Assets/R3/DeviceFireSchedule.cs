using System;
using System.Numerics;
using System.Runtime.InteropServices;

namespace RoboRougelike.R3
{
    /// <summary>
    /// Counts due device shots without allocating a projectile or iterating per
    /// shot. A consumer may take a bounded amount of work at a time; the rest
    /// stays due even if the device can no longer acquire its target.
    /// This is a scheduling primitive, not a projectile emitter.
    /// </summary>
    public sealed class DeviceFireSchedule
    {
        // Every non-negative finite float is an integer multiple of 2^-149.
        // Integer ticks avoid both counter overflow and deadline stagnation for
        // intervals that are too small to change a float/double timestamp.
        [StructLayout(LayoutKind.Explicit)]
        struct FloatBits
        {
            [FieldOffset(0)] public float Value;
            [FieldOffset(0)] public uint Bits;
        }
        BigInteger nextDue, lastTime;
        bool active;
        public BigInteger PendingShots { get; private set; }

        public void Observe(float time,float interval,bool canFire)
        {
            Validate(time,nameof(time));Validate(interval,nameof(interval));
            BigInteger now=Ticks(time);
            if(!canFire){active=false;lastTime=now;return;}
            if(!active||now<lastTime){nextDue=now;active=true;}
            lastTime=now;
            if(now<nextDue)return;
            BigInteger step=Ticks(interval);
            if(step.IsZero)
            {
                // Zero has no finite mathematical firing rate. Avoid an
                // infinite loop while preserving one activation per clock value.
                PendingShots+=BigInteger.One;nextDue=now+BigInteger.One;return;
            }
            BigInteger due=(now-nextDue)/step+BigInteger.One;
            PendingShots+=due;nextDue+=due*step;
        }

        public int Take(int maximum)
        {
            if(maximum<0)throw new ArgumentOutOfRangeException(nameof(maximum));
            int taken=(int)BigInteger.Min(PendingShots,new BigInteger(maximum));
            PendingShots-=taken;return taken;
        }

        static void Validate(float value,string name)
        {
            if(float.IsNaN(value)||float.IsInfinity(value)||value<0)
                throw new ArgumentOutOfRangeException(name,"Time and interval must be finite and non-negative.");
        }
        static BigInteger Ticks(float value)
        {
            uint bits=new FloatBits{Value=value}.Bits & 0x7fffffffu;
            int exponent=(int)((bits>>23)&0xffu);
            uint mantissa=bits&0x7fffffu;
            return exponent==0?new BigInteger(mantissa):new BigInteger(mantissa|0x800000u)<<(exponent-1);
        }
    }
}
