using UnityEngine;

[CreateAssetMenu(fileName = "AIDifficultySettings", menuName = "Minimalists/AI/AIDifficultySettings")]
public class AIDifficultySettings : ScriptableObject
{
    [Header("Decision Timing")]
    [Range(0.5f, 5f)]
    public float decisionCooldown = 2f;

    [Header("Strategic Weights")]
    [Range(0f, 1f)]
    public float aggressionChance = 0.75f;

    [Range(0.1f, 2f)]
    public float proximityWeight = 1f;

    [Header("Behavioral Thresholds")]
    [Range(0f, 50f)]
    public float reinforceThreshold = 15f;

    [Range(0f, 20f)]
    public float attackAdvantageMargin = 5f;
}