using System;

// Pure per-throw state. Contact bits 0..3 are sticks; bit 4 is the board.
public sealed class YutLandingTimer
{
    public int LandedMask { get; private set; }
    public double AllLandedAt { get; private set; } = -1;
    public void Reset() { LandedMask=0; AllLandedAt=-1; }
    public bool Tick(double now, double holdSeconds, int enteredMask, int[] contacts)
    {
        if (contacts == null || contacts.Length != 4 || double.IsNaN(now) || double.IsInfinity(now)) return false;
        int grounded=0;
        for(int i=0;i<4;i++) if((enteredMask&(1<<i))!=0 && (contacts[i]&16)!=0) grounded|=1<<i;
        for(int pass=0;pass<4;pass++)
            for(int i=0;i<4;i++)
                if((enteredMask&(1<<i))!=0 && (contacts[i]&grounded&15)!=0) grounded|=1<<i;
        LandedMask |= grounded;
        if(LandedMask!=15) return false;
        if(AllLandedAt<0) AllLandedAt=now;
        double delay=double.IsNaN(holdSeconds)||double.IsInfinity(holdSeconds)?2.5:Math.Max(0,holdSeconds);
        return now-AllLandedAt>=delay;
    }
    public static bool ReadFace(float upDot, bool lastClearFace)
    {
        // Only the numerically exact edge case uses history. No random result or pose adjustment.
        if(float.IsNaN(upDot) || float.IsInfinity(upDot)) throw new ArgumentException("Invalid face normal");
        return Math.Abs(upDot)<=0.00001f ? lastClearFace : upDot>0f;
    }
}
