using UnityEngine;

namespace FlameFighter.Hose
{
    [CreateAssetMenu(fileName = "HoseSettings", menuName = "Flame Fighter/Hose Settings")]
    public class HoseSettings : ScriptableObject
    {
        [Header("Spray Stream")]
        [Tooltip("Effective range of the water spray stream in meters.")]
        [SerializeField] private float m_SprayRange = 12f;

        [Tooltip("Radius of the spherecast used to detect fire hits.")]
        [SerializeField] private float m_SpherecastRadius = 0.35f;

        [Tooltip("Base damage dealt to fire health per second.")]
        [SerializeField] private float m_ExtinguishDPS = 40f;

        [Tooltip("Multiplier applied when spraying the base (lower half) of the fire.")]
        [SerializeField] private float m_BaseHitMultiplier = 1.75f;

        [Tooltip("Layer mask specifying what the hose stream can hit.")]
        [SerializeField] private LayerMask m_HitLayers = ~0;

        [Header("Water Tank")]
        [Tooltip("If true, water never depletes (Stage 1 tutorial mode).")]
        [SerializeField] private bool m_UnlimitedWater = true;

        [Tooltip("Maximum capacity of the water tank in liters (Stage 2).")]
        [SerializeField] private float m_MaxWaterCapacity = 100f;

        [Tooltip("Liters of water consumed per second while spraying.")]
        [SerializeField] private float m_DrainRatePerSecond = 10f;

        [Tooltip("Liters of water refilled per second at a water source.")]
        [SerializeField] private float m_RefillRatePerSecond = 35f;

        [Header("Camera & Visual Feedback")]
        [Tooltip("Angular camera kick impulse applied while spraying (x: yaw drift, y: upward kick).")]
        [SerializeField] private Vector2 m_SprayCameraKick = new Vector2(0.05f, 0.2f);

        // Public Accessors
        public float SprayRange => m_SprayRange;
        public float SpherecastRadius => m_SpherecastRadius;
        public float ExtinguishDPS => m_ExtinguishDPS;
        public float BaseHitMultiplier => m_BaseHitMultiplier;
        public LayerMask HitLayers => m_HitLayers;
        public bool UnlimitedWater => m_UnlimitedWater;
        public float MaxWaterCapacity => m_MaxWaterCapacity;
        public float DrainRatePerSecond => m_DrainRatePerSecond;
        public float RefillRatePerSecond => m_RefillRatePerSecond;
        public Vector2 SprayCameraKick => m_SprayCameraKick;
    }
}

