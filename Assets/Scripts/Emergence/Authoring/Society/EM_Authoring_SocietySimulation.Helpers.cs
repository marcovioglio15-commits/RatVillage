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
                if (string.IsNullOrWhiteSpace(source[i].ResourceId))
                    continue;

                EM_BufferElement_Resource resource = new EM_BufferElement_Resource
                {
                    ResourceId = new FixedString64Bytes(source[i].ResourceId),
                    Amount = source[i].Amount
                };

                buffer.Add(resource);
            }
        }

        private static FixedString64Bytes ToFixed(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return new FixedString64Bytes(string.Empty);

            return new FixedString64Bytes(value);
        }
        #endregion
    }
}
