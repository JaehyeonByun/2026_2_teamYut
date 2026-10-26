using System;
// Pure presentation math; no match state changes.
public static class YutScaleVisualRules
{
    public static float Angle(int playerHP,int playerMax,int opponentHP,int opponentMax,float maxAngle,bool playerOnLeft)
    {
        float p=Math.Max(0f,Math.Min(1f,(float)playerHP/Math.Max(1,playerMax)));
        float e=Math.Max(0f,Math.Min(1f,(float)opponentHP/Math.Max(1,opponentMax)));
        return (p-e)*Math.Max(0f,Math.Min(18f,maxAngle))*(playerOnLeft?1f:-1f);
    }
    public static bool DimPhase(float phase,int count)
    { return (int)Math.Floor(Math.Max(0f,Math.Min(.999999f,phase))*Math.Max(1,count)*2)%2==0; }
}
