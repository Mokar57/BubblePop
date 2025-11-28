using UnityEngine;

[CreateAssetMenu(fileName = "NewSpeedPuddleData", menuName = "PopTheBubble/Speed Puddle Data")]
public class SpeedPuddleDataSO : ScriptableObject
{
    [Header("Zone Settings")]
    public float zoneRadius = 1f;
    public float speedMultiplier = 1.5f;
    public float zoneDuration = 10f;
    public LayerMask affectedLayers = ~0;
    
    [Header("Fade Settings")]
    [Tooltip("How long it takes for the speed boost to fade out after exiting the zone")]
    public float fadeOutDuration = 2f;
    
    [Header("Visuals")]
    public bool showZoneVisual = true;
    public GameObject zoneEffect;
}
