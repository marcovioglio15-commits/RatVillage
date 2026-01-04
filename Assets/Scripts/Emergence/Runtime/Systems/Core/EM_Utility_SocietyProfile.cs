using Unity.Entities;

namespace EmergentMechanics
{
    internal static class EM_Utility_SocietyProfile
    {
        #region Utility
        // Resolves the active society profile for a subject or its society root.
        public static bool TryGetProfileReference(Entity subject, Entity societyRoot,
            ComponentLookup<EM_Component_SocietyProfileReference> profileLookup,
            out BlobAssetReference<EM_Blob_SocietyProfile> profileReference)
        {
            profileReference = default;

            if (societyRoot != Entity.Null && profileLookup.HasComponent(societyRoot))
            {
                profileReference = profileLookup[societyRoot].Value;
                return profileReference.IsCreated;
            }

            if (subject == Entity.Null || !profileLookup.HasComponent(subject))
                return false;

            profileReference = profileLookup[subject].Value;
            return profileReference.IsCreated;
        }
        #endregion
    }
}
