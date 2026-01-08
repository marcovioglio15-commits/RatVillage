using TMPro;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;

namespace EmergentMechanics
{
    [DisallowMultipleComponent]
    public sealed class EM_SimulationSpeedUi : MonoBehaviour
    {
        private const string DefaultLabelFormat = "Sim Speed: {0:0.0}x";

        [Header("Simulation Speed")]
        [SerializeField] private float minMultiplier = 0.1f;
        [SerializeField] private float maxMultiplier = 10f;
        [SerializeField] private string labelFormat = DefaultLabelFormat;

        [Header("References")]
        [Tooltip("Slider used to control the simulation speed multiplier.")]
        [SerializeField] private Slider slider;
        [Tooltip("Label used to display the current multiplier.")]
        [SerializeField] private TMP_Text label;

        private World cachedWorld;
        private EntityManager entityManager;
        private EntityQuery clockQuery;
        private bool suppressCallbacks;
        private bool hasReferences;

        private void Awake()
        {
            hasReferences = ValidateReferences();

            if (!hasReferences)
                return;

            ConfigureSlider();
        }

        private void OnEnable()
        {
            if (!hasReferences)
                hasReferences = ValidateReferences();

            if (!hasReferences)
                return;

            slider.onValueChanged.AddListener(HandleSliderChanged);
            SyncFromClock();
        }

        private void OnDisable()
        {
            if (!hasReferences)
                return;

            slider.onValueChanged.RemoveListener(HandleSliderChanged);
        }

        private void ConfigureSlider()
        {
            if (slider == null)
                return;

            float min = Mathf.Min(minMultiplier, maxMultiplier);
            float max = Mathf.Max(minMultiplier, maxMultiplier);

            if (Mathf.Approximately(min, max))
                max = min + 1f;

            slider.minValue = min;
            slider.maxValue = max;
            slider.wholeNumbers = false;
        }

        private void HandleSliderChanged(float value)
        {
            if (suppressCallbacks)
                return;

            float clamped = ClampMultiplier(value);
            ApplyMultiplier(clamped);
            UpdateLabel(clamped);

            if (!Mathf.Approximately(value, clamped) && slider != null)
            {
                suppressCallbacks = true;
                slider.SetValueWithoutNotify(clamped);
                suppressCallbacks = false;
            }
        }

        private void SyncFromClock()
        {
            if (!hasReferences)
                return;

            float multiplier = 1f;

            if (TryGetMultiplier(out float current))
                multiplier = current;

            multiplier = ClampMultiplier(multiplier);

            if (slider != null)
            {
                suppressCallbacks = true;
                slider.SetValueWithoutNotify(multiplier);
                suppressCallbacks = false;
            }

            UpdateLabel(multiplier);
        }

        private float ClampMultiplier(float value)
        {
            float min = Mathf.Min(minMultiplier, maxMultiplier);
            float max = Mathf.Max(minMultiplier, maxMultiplier);

            if (Mathf.Approximately(min, max))
                max = min + 1f;

            return Mathf.Clamp(value, min, max);
        }

        private void UpdateLabel(float multiplier)
        {
            if (label == null)
                return;

            label.text = FormatLabel(multiplier);
        }

        private string FormatLabel(float multiplier)
        {
            string template = string.IsNullOrEmpty(labelFormat) ? DefaultLabelFormat : labelFormat;
            return string.Format(template, multiplier);
        }

        private bool TryGetMultiplier(out float multiplier)
        {
            multiplier = 1f;

            if (!EnsureWorld())
                return false;

            NativeArray<EM_Component_SocietyClock> clocks = clockQuery.ToComponentDataArray<EM_Component_SocietyClock>(Allocator.Temp);

            if (clocks.Length == 0)
            {
                clocks.Dispose();
                return false;
            }

            multiplier = clocks[0].SimulationSpeedMultiplier;
            clocks.Dispose();
            return true;
        }

        /// <summary>
        /// Applies the specified simulation speed multiplier to all entities managed by the clock query.
        /// </summary>
        /// <remarks>This method updates the <c>SimulationSpeedMultiplier</c> property of each entity's
        /// <c>EM_Component_SocietyClock</c> component. If the world context is not available, the method performs no
        /// action.</remarks>
        /// <param name="multiplier">The value to set as the simulation speed multiplier for each entity. Typically, values greater than 1.0
        /// increase simulation speed, while values less than 1.0 decrease it.</param>
        private void ApplyMultiplier(float multiplier)
        {
            if (!EnsureWorld())
                return;

            NativeArray<Entity> entities = clockQuery.ToEntityArray(Allocator.Temp);

            for (int i = 0; i < entities.Length; i++)
            {
                Entity entity = entities[i];
                EM_Component_SocietyClock clock = entityManager.GetComponentData<EM_Component_SocietyClock>(entity);
                clock.SimulationSpeedMultiplier = multiplier;
                entityManager.SetComponentData(entity, clock);
            }

            entities.Dispose();
        }

        private bool EnsureWorld()
        {
            if (cachedWorld != null && cachedWorld.IsCreated)
                return true;

            cachedWorld = World.DefaultGameObjectInjectionWorld;

            if (cachedWorld == null || !cachedWorld.IsCreated)
                return false;

            entityManager = cachedWorld.EntityManager;
            clockQuery = entityManager.CreateEntityQuery(ComponentType.ReadWrite<EM_Component_SocietyClock>(), ComponentType.ReadOnly<EM_Component_SocietyRoot>());
            return true;
        }

        private bool ValidateReferences()
        {
            if (slider == null || label == null)
            {
                Debug.LogError("EM_SimulationSpeedUi is missing references. Assign a Slider and TMP_Text in the inspector.", this);
                enabled = false;
                return false;
            }

            return true;
        }
    }
}
