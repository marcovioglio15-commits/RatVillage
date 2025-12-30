using Unity.Entities;

namespace EmergentMechanics
{
    public struct EM_Component_LibraryReference : IComponentData
    {
        #region Data
        public BlobAssetReference<EM_Blob_Library> Value;
        #endregion
    }
}
