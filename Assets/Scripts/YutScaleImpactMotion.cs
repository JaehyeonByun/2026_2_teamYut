using System;
// Deterministic presentation curves, independent of frame rate and Unity.
public static class YutScaleImpactMotion
{
    private static float Clamp(float v,float lo,float hi) { return Math.Max(lo,Math.Min(hi,v)); }
    public static float Gust(float progress)
    {
        float t=Clamp(progress,0,1);
        float envelope=(float)Math.Sin(Math.PI*t)*(1f-.35f*t);
        return envelope*(.75f+.25f*(float)Math.Sin(5*Math.PI*t));
    }
    public static float Angle(float start,float target,float elapsed,float snap,float settle,float overshoot)
    {
        snap=Math.Max(.05f,snap);settle=Math.Max(.05f,settle);
        target=Clamp(target,-18,18);start=Clamp(start,-18,18);
        float sign=Math.Sign(target-start);
        float peak=Clamp(target+sign*Math.Max(0,overshoot),-18,18);
        if(elapsed<=0) return start;
        if(elapsed<snap)
        { float t=Clamp(elapsed/snap,0,1);float ease=1-(1-t)*(1-t)*(1-t);return start+(peak-start)*ease; }
        float u=Clamp((elapsed-snap)/settle,0,1);
        float rebound=(peak-target)*(float)Math.Cos(2*Math.PI*u)*(1-u)*(1-u)
            -sign*1.3f*(float)Math.Sin(Math.PI*u)*(1-u);
        return Clamp(target+rebound,-18,18);
    }
}
