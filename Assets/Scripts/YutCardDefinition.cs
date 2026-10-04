using UnityEngine;

public enum YutCardEffect { RestoreHealth }

[CreateAssetMenu(fileName = "SoulMend", menuName = "Yut/Card Definition")]
public sealed class YutCardDefinition : ScriptableObject
{
    public string displayName = "Soul Mend";
    public YutCardEffect effect = YutCardEffect.RestoreHealth;
    [Min(1)] public int amount = 20;
    [Min(0)] public int startingCopiesPerSide = 1;
    [TextArea] public string description = "Restore 20 HP. Once per turn. Consumed on use.";
}
