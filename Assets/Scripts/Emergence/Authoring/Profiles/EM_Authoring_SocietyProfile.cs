using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace EmergentMechanics
{
    public sealed class EM_Authoring_SocietyProfile : MonoBehaviour
    {
        #region Fields
        // Serialized inputs for society profile authoring.
        #region Serialized
        #region References
        [Tooltip("Library that owns all signals, rule sets, effects, metrics, and domains referenced by this profile. Place this component on the society root GameObject and keep it consistent with EM_Authoring_Library.")]
        [Header("References")]
        [SerializeField] private EM_MechanicLibrary library;

        [Tooltip("Society profile applied to the society root. Controls enabled domains, rule sets, metrics, and scalability settings.")]
        [SerializeField] private EM_SocietyProfile profile;

        [Tooltip("Optional debug name used by the Emergence debug HUD. Leave empty to use the society entity id.")]
        [SerializeField] private string LogName;
        #endregion
        #endregion

        #region Public Properties Properties
        public EM_MechanicLibrary Library
        {
            get
            {
                return library;
            }
        }

        public EM_SocietyProfile Profile
        {
            get
            {
                return profile;
            }
        }

        #endregion
        #endregion

        #region Baker
        public sealed class Baker : Baker<EM_Authoring_SocietyProfile>
        {
            public override void Bake(EM_Authoring_SocietyProfile authoring)
            {
                if (authoring.library == null || authoring.profile == null)
                    return;

                Entity entity = GetEntity(TransformUsageFlags.None);

                BlobAssetReference<EM_Blob_SocietyProfile> profileBlob = EM_BlobBuilder_SocietyProfile.BuildProfileBlob(authoring.profile, authoring.library);

                if (!profileBlob.IsCreated)
                    return;

                AddBlobAsset(ref profileBlob, out Unity.Entities.Hash128 _);
                AddComponent(entity, new EM_Component_SocietyProfileReference { Value = profileBlob });
                AddComponent<EM_Component_SocietyRoot>(entity);
                AddComponent(entity, new EM_Component_RandomSeed { Value = GetStableSeed(authoring.name) });
                AddBuffer<EM_BufferElement_MetricAccumulator>(entity);
                AddBuffer<EM_BufferElement_MetricTimer>(entity);
                AddBuffer<EM_BufferElement_MetricEventSample>(entity);
                AddBuffer<EM_BufferElement_RuleCooldown>(entity);

                if (!string.IsNullOrWhiteSpace(authoring.LogName))
                {
                    EM_Component_NpcType LogNameComponent = new EM_Component_NpcType
                    {
                        TypeId = new FixedString64Bytes(authoring.LogName)
                    };

                    AddComponent(entity, LogNameComponent);
                }
            }
        }
        #endregion

        #region Helpers
        // Build a deterministic seed from the authoring object name.
        private static uint GetStableSeed(string fallback)
        {
            if (string.IsNullOrWhiteSpace(fallback))
                return 1u;

            FixedString64Bytes fixedName = new FixedString64Bytes(fallback);
            uint hashed = (uint)fixedName.GetHashCode();

            if (hashed == 0u)
                return 1u;

            return hashed;
        }
        #endregion
    }
}
