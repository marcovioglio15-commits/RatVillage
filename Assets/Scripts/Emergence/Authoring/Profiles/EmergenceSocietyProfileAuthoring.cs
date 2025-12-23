using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Emergence
{
    /// <summary>
    /// Authoring component that bakes a society profile onto a society root entity.
    /// </summary>
    public sealed class EmergenceSocietyProfileAuthoring : MonoBehaviour
    {
        #region Serialized
        // Serialized references
        #region Serialized - References
        [Tooltip("Library that owns all signals, rule sets, effects, metrics, and domains referenced by this profile. Place this component on the society root GameObject and keep it consistent with EmergenceLibraryAuthoring.")]
        [SerializeField] private EM_MechanicLibrary library;

        [Tooltip("Society profile applied to the society root. Controls enabled domains, rule sets, metrics, and scalability settings.")]
        [SerializeField] private EM_SocietyProfile profile;

        [Tooltip("Optional debug name used by the Emergence debug HUD. Leave empty to use the society entity id.")]
        [SerializeField] private string debugName;
        #endregion

        // Serialized defaults
        #region Serialized - Defaults
        [Tooltip("Initial LOD tier for this society.")]
        [SerializeField] private EmergenceLodTier initialLod = EmergenceLodTier.Full;
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
        /// Gets the society profile reference.
        /// </summary>
        public EM_SocietyProfile Profile
        {
            get
            {
                return profile;
            }
        }

        /// <summary>
        /// Gets the initial LOD tier.
        /// </summary>
        public EmergenceLodTier InitialLod
        {
            get
            {
                return initialLod;
            }
        }
        #endregion

        #region Baker
        /// <summary>
        /// Bakes the society profile data into ECS components.
        /// </summary>
        public sealed class Baker : Baker<EmergenceSocietyProfileAuthoring>
        {
            /// <summary>
            /// Executes the baking process.
            /// </summary>
            public override void Bake(EmergenceSocietyProfileAuthoring authoring)
            {
                if (authoring.library == null || authoring.profile == null)
                    return;

                Entity entity = GetEntity(TransformUsageFlags.None);

                BlobAssetReference<EmergenceSocietyProfileBlob> profileBlob = EmergenceSocietyProfileBlobBuilder.BuildProfileBlob(authoring.profile, authoring.library);

                if (!profileBlob.IsCreated)
                    return;

                AddBlobAsset(ref profileBlob, out Unity.Entities.Hash128 _);
                AddComponent(entity, new EmergenceSocietyProfileReference { Value = profileBlob });
                AddComponent<EmergenceSocietyRoot>(entity);
                AddComponent(entity, new EmergenceSocietyLod { Tier = authoring.initialLod });
                AddBuffer<EmergenceMetricTimer>(entity);

                if (!string.IsNullOrWhiteSpace(authoring.debugName))
                {
                    EmergenceDebugName debugNameComponent = new EmergenceDebugName
                    {
                        Value = new FixedString64Bytes(authoring.debugName)
                    };

                    AddComponent(entity, debugNameComponent);
                }
            }
        }
        #endregion
    }
}
