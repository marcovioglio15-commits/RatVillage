using Unity.Collections;
using Unity.Entities;

namespace EmergentMechanics
{
    public sealed partial class EM_Authoring_SocietySimulation
    {
        #region Helpers
        private static void AddSocietyResources(ResourceEntry[] source, ref DynamicBuffer<EM_BufferElement_Resource> buffer)
        {
            if (source == null)
                return;

            for (int i = 0; i < source.Length; i++)
            {
                if (!EM_IdUtility.HasId(source[i].ResourceIdDefinition, source[i].ResourceId))
                    continue;

                EM_BufferElement_Resource resource = new EM_BufferElement_Resource
                {
                    ResourceId = EM_IdUtility.ToFixed(source[i].ResourceIdDefinition, source[i].ResourceId),
                    Amount = source[i].Amount
                };

                buffer.Add(resource);
            }
        }

        private static void AddNeedSignalOverrides(NeedSignalOverrideEntry[] source, ref DynamicBuffer<EM_BufferElement_NeedSignalOverride> buffer)
        {
            if (source == null)
                return;

            for (int i = 0; i < source.Length; i++)
            {
                if (!EM_IdUtility.HasId(source[i].NeedIdDefinition, source[i].NeedId))
                    continue;

                EM_BufferElement_NeedSignalOverride entry = new EM_BufferElement_NeedSignalOverride
                {
                    NeedId = EM_IdUtility.ToFixed(source[i].NeedIdDefinition, source[i].NeedId),
                    ValueSignalId = EM_IdUtility.ToFixed(source[i].ValueSignalIdDefinition, source[i].ValueSignalId),
                    UrgencySignalId = EM_IdUtility.ToFixed(source[i].UrgencySignalIdDefinition, source[i].UrgencySignalId)
                };

                buffer.Add(entry);
            }
        }

        #endregion
    }
}
