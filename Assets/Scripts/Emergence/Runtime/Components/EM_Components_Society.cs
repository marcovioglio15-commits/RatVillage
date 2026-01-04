using Unity.Collections;
using Unity.Entities;

namespace EmergentMechanics
{
    public struct EM_Component_SocietyRoot : IComponentData
    {
    }

    public struct EM_Component_SocietyMember : IComponentData
    {
        #region Data
        public Entity SocietyRoot;
        #endregion
    }

    public struct EM_Component_SocietyProfileReference : IComponentData
    {
        #region Data
        public BlobAssetReference<EM_Blob_SocietyProfile> Value;
        #endregion
    }

    public struct EM_BufferElement_Need : IBufferElementData
    {
        #region Data
        public FixedString64Bytes NeedId;
        public float Value;
        #endregion
    }

    public struct EM_BufferElement_Resource : IBufferElementData
    {
        #region Data
        public FixedString64Bytes ResourceId;
        public float Amount;
        #endregion
    }

    public struct EM_Component_Reputation : IComponentData
    {
        #region Data
        public float Value;
        #endregion
    }

    public struct EM_Component_Cohesion : IComponentData
    {
        #region Data
        public float Value;
        #endregion
    }

}
