using Unity.Entities;
using UnityEngine;

namespace Emergence
{
    /// <summary>
    /// Authoring component that bakes the emergence library into a blob asset.
    /// </summary>
    public sealed class EmergenceLibraryAuthoring : MonoBehaviour
    {
        #region Constants
        private const float DefaultFullSimTickRate = 20f;
        private const float DefaultSimplifiedSimTickRate = 5f;
        private const float DefaultAggregatedSimTickRate = 1f;
        private const int DefaultMaxSignalQueue = 2048;
        #endregion

        #region Serialized
        // Serialized references
        #region Serialized - References
        [Tooltip("Library asset containing all Emergence definitions (signals, rule sets, effects, metrics, domains, profiles). This is the global registry baked for runtime lookups.")]
        [SerializeField] private EM_MechanicLibrary library;

        [Tooltip("Optional default profile used to set global tick rates and signal queue limits when no per-society overrides are present.")]
        [SerializeField] private EM_SocietyProfile defaultProfile;
        #endregion
        #endregion

        #region Public
        /// <summary>
        /// Gets the library asset reference.
        /// </summary>
        public EM_MechanicLibrary Library
        {
            get
            {
                return library;
            }
        }

        /// <summary>
        /// Gets the default profile reference.
        /// </summary>
        public EM_SocietyProfile DefaultProfile
        {
            get
            {
                return defaultProfile;
            }
        }
        #endregion

        #region Baker
        /// <summary>
        /// Bakes the emergence library data into ECS components.
        /// </summary>
        public sealed class Baker : Baker<EmergenceLibraryAuthoring>
        {
            /// <summary>
            /// Executes the baking process.
            /// </summary>
            public override void Bake(EmergenceLibraryAuthoring authoring)
            {
                if (authoring.library == null)
                    return;

                Entity entity = GetEntity(TransformUsageFlags.None);

                BlobAssetReference<EmergenceLibraryBlob> libraryBlob = EmergenceLibraryBlobBuilder.BuildLibraryBlob(authoring.library);

                if (!libraryBlob.IsCreated)
                    return;

                AddBlobAsset(ref libraryBlob, out Unity.Entities.Hash128 _);
                AddComponent(entity, new EmergenceLibraryReference { Value = libraryBlob });

                EmergenceGlobalSettings settings = BuildSettings(authoring.defaultProfile);
                AddComponent(entity, settings);
                AddComponent<EmergenceTierTickState>(entity);
                AddBuffer<EmergenceSignalEvent>(entity);
                AddBuffer<EmergenceMetricSample>(entity);
            }
        }
        #endregion

        #region Helpers
        private static EmergenceGlobalSettings BuildSettings(EM_SocietyProfile profile)
        {
            EmergenceGlobalSettings settings = new EmergenceGlobalSettings
            {
                MaxSignalQueue = DefaultMaxSignalQueue,
                FullSimTickRate = DefaultFullSimTickRate,
                SimplifiedSimTickRate = DefaultSimplifiedSimTickRate,
                AggregatedSimTickRate = DefaultAggregatedSimTickRate
            };

            if (profile == null)
                return settings;

            settings.MaxSignalQueue = profile.MaxSignalQueue;
            settings.FullSimTickRate = profile.FullSimTickRate;
            settings.SimplifiedSimTickRate = profile.SimplifiedSimTickRate;
            settings.AggregatedSimTickRate = profile.AggregatedSimTickRate;

            return settings;
        }
        #endregion
    }
}
