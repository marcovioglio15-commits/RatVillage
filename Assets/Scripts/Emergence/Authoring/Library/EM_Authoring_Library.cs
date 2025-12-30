using Unity.Entities;
using UnityEngine;

namespace EmergentMechanics
{
    /// <summary>
    /// Provides authoring-time configuration for Emergence mechanics by exposing references to the global Emergence
    /// library asset. This component enables runtime systems to access definitions for signals, metrics, rule sets,
    /// effects, domains, and profiles within the Emergence framework.
    /// </summary>
    /// <remarks>
    /// Attach this component to a GameObject to supply Emergence runtime systems with the necessary
    /// library data. The referenced library asset acts as a global registry for Emergence definitions.
    /// </remarks>
    public sealed class EM_Authoring_Library : MonoBehaviour
    {
        #region Fields
        #region Serialized
        [Tooltip("Library asset containing all Emergence definitions (signals, metrics, rule sets, effects, domains, profiles). This is the global registry baked for runtime lookups.")]
        [Header("Library")]
        [SerializeField] private EM_MechanicLibrary library;
        #endregion

        #region Public Properties Properties
        public EM_MechanicLibrary Library
        {
            get
            {
                return library;
            }
        }

        #endregion
        #endregion

        #region Methods
        #region Baker
        /// <summary>
        /// Provides functionality to convert an <see cref="EM_Authoring_Library"/> authoring component into its
        /// corresponding runtime entity representation during the baking process.
        /// </summary>
        /// <remarks>
        /// The <c>Baker</c> class is used within the Unity Entities workflow to transform
        /// authoring data into optimized runtime data. It processes the provided <see cref="EM_Authoring_Library"/>
        /// instance, creates the necessary blob assets, and attaches required components and buffers to the resulting
        /// entity. This class is sealed and intended for use by the Unity baking system.
        /// </remarks>
        public sealed class Baker : Baker<EM_Authoring_Library>
        {
            /// <summary>
            /// Executes the baking process.
            /// </summary>
            public override void Bake(EM_Authoring_Library authoring)
            {
                if (authoring.library == null)
                    return;

                Entity entity = GetEntity(TransformUsageFlags.None);

                BlobAssetReference<EM_Blob_Library> libraryBlob = EM_BlobBuilder_Library.BuildLibraryBlob(authoring.library);

                if (!libraryBlob.IsCreated)
                    return;

                AddBlobAsset(ref libraryBlob, out Unity.Entities.Hash128 _);
                AddComponent(entity, new EM_Component_LibraryReference { Value = libraryBlob });
                AddBuffer<EM_BufferElement_MetricSample>(entity);
            }
        }
        #endregion
        #endregion
    }
}
