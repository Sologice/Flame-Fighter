using System;
using UnityEngine;

namespace FlameFighter.Fire
{
    public enum BurnState
    {
        Unlit,
        Burning,
        Extinguished,
        BurnedOut
    }

    [SelectionBase]
    public class Flammable : MonoBehaviour
    {
        [Header("Fire Health & Extinguishing")]
        [Tooltip("Maximum health of the fire. Water drains this health.")]
        [SerializeField] private float m_MaxFireHealth = 100f;

        [Tooltip("The lower proportion of the object's height considered the 'base' (0.0 to 1.0).")]
        [Range(0.1f, 0.9f)]
        [SerializeField] private float m_BaseHeightRatio = 0.4f;

        [Header("Fuel & Longevity")]
        [Tooltip("Total burning duration in seconds before turning to ash/burned out.")]
        [SerializeField] private float m_MaxFuel = 60f;

        [Tooltip("Initial burn state on spawn.")]
        [SerializeField] private BurnState m_InitialState = BurnState.Burning;

        [Tooltip("Time in seconds an extinguished object remains wet before nearby flames can reignite it.")]
        [SerializeField] private float m_ReignitionCooldown = 6f;

        // Current runtime states
        private float m_CurrentFireHealth;
        private float m_CurrentFuel;
        private BurnState m_State;
        private float m_CooldownTimer;
        private Collider m_Collider;

        // Public Events
        public event Action<Flammable> OnIgnited;
        public event Action<Flammable> OnExtinguished;
        public event Action<Flammable> OnBurnedOut;
        public event Action<Vector3, bool> OnHitByWater; // Vector3 hitPoint, bool isBaseHit

        // Public Properties
        public BurnState State => m_State;
        public bool IsBurning => m_State == BurnState.Burning;
        public float FireHealthRatio => m_MaxFireHealth > 0f ? Mathf.Clamp01(m_CurrentFireHealth / m_MaxFireHealth) : 0f;
        public float FuelRatio => m_MaxFuel > 0f ? Mathf.Clamp01(m_CurrentFuel / m_MaxFuel) : 0f;
        public float FireIntensity => (m_State == BurnState.Burning) ? Mathf.Min(FireHealthRatio, 1f) : 0f;

        private void Awake()
        {
            m_Collider = GetComponent<Collider>();
            m_CurrentFuel = m_MaxFuel;

            if (m_InitialState == BurnState.Burning)
            {
                Ignite();
            }
            else
            {
                m_State = m_InitialState;
                m_CurrentFireHealth = 0f;
            }
        }

        private void Update()
        {
            if (m_State == BurnState.Burning)
            {
                // Consume fuel
                m_CurrentFuel -= Time.deltaTime;
                if (m_CurrentFuel <= 0f)
                {
                    m_CurrentFuel = 0f;
                    SetBurnedOut();
                }
            }
            else if (m_State == BurnState.Extinguished)
            {
                // Count down wetness cooldown
                if (m_CooldownTimer > 0f)
                {
                    m_CooldownTimer -= Time.deltaTime;
                }
            }
        }

        /// <summary>
        /// Ignites this flammable object.
        /// </summary>
        public bool Ignite()
        {
            if (m_State == BurnState.BurnedOut) return false;
            if (m_State == BurnState.Extinguished && m_CooldownTimer > 0f) return false;

            m_State = BurnState.Burning;
            m_CurrentFireHealth = m_MaxFireHealth;
            OnIgnited?.Invoke(this);
            return true;
        }

        /// <summary>
        /// Applies water stream damage to the fire.
        /// Returns true if hit resulted in extinguishing the fire.
        /// </summary>
        public bool Extinguish(float damage, Vector3 hitPoint, float baseBonusMultiplier = 1.75f)
        {
            if (m_State != BurnState.Burning) return false;

            // Check if hitting the base of the fire
            bool isBaseHit = CheckIsBaseHit(hitPoint);
            float finalDamage = damage * (isBaseHit ? baseBonusMultiplier : 1.0f);

            m_CurrentFireHealth -= finalDamage;
            OnHitByWater?.Invoke(hitPoint, isBaseHit);

            if (m_CurrentFireHealth <= 0f)
            {
                m_CurrentFireHealth = 0f;
                SetExtinguished();
                return true;
            }

            return false;
        }

        private bool CheckIsBaseHit(Vector3 hitPoint)
        {
            if (m_Collider == null) m_Collider = GetComponent<Collider>();
            if (m_Collider == null) return false;

            Bounds bounds = m_Collider.bounds;
            float baseThresholdY = bounds.min.y + (bounds.size.y * m_BaseHeightRatio);
            return hitPoint.y <= baseThresholdY;
        }

        private void SetExtinguished()
        {
            m_State = BurnState.Extinguished;
            m_CooldownTimer = m_ReignitionCooldown;
            OnExtinguished?.Invoke(this);
        }

        private void SetBurnedOut()
        {
            m_State = BurnState.BurnedOut;
            m_CurrentFireHealth = 0f;
            OnBurnedOut?.Invoke(this);
        }

        public void ForceExtinguish()
        {
            m_CurrentFireHealth = 0f;
            SetExtinguished();
        }
    }
}

