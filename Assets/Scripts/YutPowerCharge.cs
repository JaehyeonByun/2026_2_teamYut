using System;

// Pure charge state. Cancel never produces a throw; each hold can release once.
public sealed class YutPowerCharge
{
    public bool IsHolding { get; private set; }
    private double started;
    public bool Begin(double now)
    {
        if(IsHolding || double.IsNaN(now) || double.IsInfinity(now)) return false;
        started=now;IsHolding=true;return true;
    }
    public float Value(double now,float chargeSeconds)
    {
        if(!IsHolding || double.IsNaN(now) || double.IsInfinity(now)) return 0f;
        double duration=float.IsNaN(chargeSeconds)||float.IsInfinity(chargeSeconds)?1.2:Math.Max(0.1,chargeSeconds);
        return (float)Math.Max(0,Math.Min(1,(now-started)/duration));
    }
    public bool Release(double now,float chargeSeconds,out float power)
    {
        power=0f;if(!IsHolding) return false;
        power=Value(now,chargeSeconds);IsHolding=false;return true;
    }
    public void Cancel(){IsHolding=false;}
}
