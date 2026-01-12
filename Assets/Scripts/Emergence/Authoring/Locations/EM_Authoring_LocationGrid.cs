using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace EmergentMechanics
{
    // Authoring component for editable grid-based locations.
    public sealed class EM_Authoring_LocationGrid : MonoBehaviour
    {
        #region Nested Types
        [Serializable]
        public struct LocationNode
        {
            #region Data
            [Tooltip("Whether the node is walkable by NPCs.")]
            [SerializeField] private bool walkable;

            [Tooltip("Location definition assigned to this node. When set, the node becomes a target location.")]
            [SerializeField] private EM_LocationDefinition locationDefinition;
            #endregion

            #region Properties
            public bool Walkable
            {
                get
                {
                    return walkable;
                }
            }

            public EM_LocationDefinition LocationDefinition
            {
                get
                {
                    return locationDefinition;
                }
            }
            #endregion

            #region Methods
            public static LocationNode CreateDefault(bool isWalkable)
            {
                LocationNode node = new LocationNode
                {
                    walkable = isWalkable,
                    locationDefinition = null
                };

                return node;
            }
            #endregion
        }
        #endregion

        #region Fields
        #region Serialized
        [Tooltip("Grid width in nodes.")]
        [Header("Grid")]
        [SerializeField] private int width = 10;

        [Tooltip("Grid height in nodes.")]
        [SerializeField] private int height = 10;

        [Tooltip("Size of a grid node in world units.")]
        [SerializeField] private float nodeSize = 1f;

        [Tooltip("World-space offset from the GameObject position to the grid center.")]
        [SerializeField] private Vector3 originOffset = Vector3.zero;

        [Tooltip("Draw grid gizmos when the object is selected.")]
        [SerializeField] private bool drawGizmos = true;

        [Tooltip("Draw cell coordinate labels when gizmos are enabled.")]
        [SerializeField] private bool showCellLabels = true;

        [Tooltip("Color used for walkable nodes without a location assigned.")]
        [SerializeField] private Color walkableColor = new Color(0.2f, 0.7f, 0.2f, 0.2f);

        [Tooltip("Color used for unwalkable nodes.")]
        [SerializeField] private Color blockedColor = new Color(0.8f, 0.2f, 0.2f, 0.2f);

        [Tooltip("Color used to draw grid cell borders.")]
        [SerializeField] private Color gridLineColor = new Color(1f, 1f, 1f, 0.6f);

        [Tooltip("Serialized grid nodes for editor painting.")]
        [SerializeField]
        [HideInInspector] private LocationNode[] nodes = new LocationNode[0];
        #endregion
        #endregion

        #region Unity Lifecycle
        private void OnValidate()
        {
            if (width < 1)
                width = 1;

            if (height < 1)
                height = 1;

            if (nodeSize <= 0f)
                nodeSize = 0.1f;

            int expected = width * height;

            if (nodes == null || nodes.Length != expected)
                nodes = ResizeNodes(nodes, expected, true);
        }

        private void OnDrawGizmosSelected()
        {
            if (!drawGizmos)
                return;

            int drawWidth = math.max(1, width);
            int drawHeight = math.max(1, height);
            float drawNodeSize = math.max(0.1f, nodeSize);
            Vector3 origin = ResolveGridOrigin(transform.position, originOffset, drawWidth, drawHeight, drawNodeSize);

            if (nodes == null || nodes.Length != drawWidth * drawHeight)
                return;

            Vector3 size = new Vector3(drawNodeSize, 0.05f, drawNodeSize);
            DrawGridLines(origin, drawWidth, drawHeight, drawNodeSize);

            for (int y = 0; y < drawHeight; y++)
            {
                for (int x = 0; x < drawWidth; x++)
                {
                    int index = x + y * drawWidth;
                    LocationNode node = nodes[index];
                    Vector3 center = origin + new Vector3((x + 0.5f) * drawNodeSize, 0f, (y + 0.5f) * drawNodeSize);
                    Color color = node.Walkable ? walkableColor : blockedColor;

                    if (node.LocationDefinition != null)
                        color = node.LocationDefinition.Color;

                    Gizmos.color = color;
                    Gizmos.DrawCube(center, size);

#if UNITY_EDITOR
                    if (showCellLabels)
                        DrawCellLabel(center, drawNodeSize, x, y);
#endif
                }
            }
        }
        #endregion

        #region Helpers
#if UNITY_EDITOR
        private static GUIStyle cellLabelStyle;
#endif

        private static Vector3 ResolveGridOrigin(Vector3 position, Vector3 offset, int width, int height, float nodeSize)
        {
            float widthSize = width * nodeSize;
            float heightSize = height * nodeSize;
            Vector3 center = position + offset;
            Vector3 half = new Vector3(widthSize * 0.5f, 0f, heightSize * 0.5f);

            return center - half;
        }

        private void DrawGridLines(Vector3 origin, int width, int height, float nodeSize)
        {
            Gizmos.color = gridLineColor;
            float widthSize = width * nodeSize;
            float heightSize = height * nodeSize;
            float lineHeight = math.max(0.01f, nodeSize * 0.02f);
            Vector3 lineOffset = new Vector3(0f, lineHeight, 0f);
            Vector3 bottomLeft = origin + lineOffset;
            Vector3 bottomRight = origin + new Vector3(widthSize, 0f, 0f) + lineOffset;
            Vector3 topLeft = origin + new Vector3(0f, 0f, heightSize) + lineOffset;
            Vector3 topRight = origin + new Vector3(widthSize, 0f, heightSize) + lineOffset;

            Gizmos.DrawLine(bottomLeft, bottomRight);
            Gizmos.DrawLine(bottomLeft, topLeft);
            Gizmos.DrawLine(topLeft, topRight);
            Gizmos.DrawLine(bottomRight, topRight);

            for (int x = 1; x < width; x++)
            {
                Vector3 start = origin + new Vector3(x * nodeSize, 0f, 0f) + lineOffset;
                Vector3 end = origin + new Vector3(x * nodeSize, 0f, heightSize) + lineOffset;
                Gizmos.DrawLine(start, end);
            }

            for (int y = 1; y < height; y++)
            {
                Vector3 start = origin + new Vector3(0f, 0f, y * nodeSize) + lineOffset;
                Vector3 end = origin + new Vector3(widthSize, 0f, y * nodeSize) + lineOffset;
                Gizmos.DrawLine(start, end);
            }
        }

#if UNITY_EDITOR
        private static GUIStyle GetCellLabelStyle()
        {
            if (cellLabelStyle != null)
                return cellLabelStyle;

            cellLabelStyle = new GUIStyle(UnityEditor.EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter
            };
            cellLabelStyle.normal.textColor = Color.white;
            return cellLabelStyle;
        }

        private void DrawCellLabel(Vector3 center, float nodeSize, int x, int y)
        {
            float heightOffset = math.max(0.02f, nodeSize * 0.05f);
            Vector3 labelPosition = center + new Vector3(0f, heightOffset, 0f);
            GUIStyle style = GetCellLabelStyle();
            UnityEditor.Handles.Label(labelPosition, string.Format("{0},{1}", x, y), style);
        }
#endif

        private static LocationNode[] ResizeNodes(LocationNode[] source, int size, bool defaultWalkable)
        {
            LocationNode[] result = new LocationNode[size];
            int copyCount = 0;

            if (source != null)
                copyCount = math.min(source.Length, size);

            for (int i = 0; i < copyCount; i++)
                result[i] = source[i];

            for (int i = copyCount; i < size; i++)
                result[i] = LocationNode.CreateDefault(defaultWalkable);

            return result;
        }
        #endregion

        #region Baker
        public sealed class Baker : Baker<EM_Authoring_LocationGrid>
        {
            public override void Bake(EM_Authoring_LocationGrid authoring)
            {
                Entity gridEntity = GetEntity(TransformUsageFlags.None);
                int gridWidth = math.max(1, authoring.width);
                int gridHeight = math.max(1, authoring.height);
                float gridNodeSize = math.max(0.1f, authoring.nodeSize);
                Vector3 origin = ResolveGridOrigin(authoring.transform.position, authoring.originOffset, gridWidth, gridHeight, gridNodeSize);
                float3 gridOrigin = (float3)origin;

                AddComponent(gridEntity, new EM_Component_LocationGrid
                {
                    Width = gridWidth,
                    Height = gridHeight,
                    NodeSize = gridNodeSize,
                    Origin = gridOrigin
                });

                DynamicBuffer<EM_BufferElement_LocationNode> nodeBuffer = AddBuffer<EM_BufferElement_LocationNode>(gridEntity);
                DynamicBuffer<EM_BufferElement_LocationOccupancy> occupancyBuffer = AddBuffer<EM_BufferElement_LocationOccupancy>(gridEntity);
                int nodeCount = gridWidth * gridHeight;
                nodeBuffer.ResizeUninitialized(nodeCount);
                occupancyBuffer.ResizeUninitialized(nodeCount);

                for (int i = 0; i < nodeCount; i++)
                {
                    LocationNode node = default;

                    if (authoring.nodes != null && i < authoring.nodes.Length)
                        node = authoring.nodes[i];

                    FixedString64Bytes locationId = default;
                    EM_LocationDefinition locationDefinition = node.LocationDefinition;

                    if (locationDefinition != null && !string.IsNullOrWhiteSpace(locationDefinition.Id))
                        locationId = new FixedString64Bytes(locationDefinition.Id);

                    nodeBuffer[i] = new EM_BufferElement_LocationNode
                    {
                        Walkable = (byte)(node.Walkable ? 1 : 0),
                        LocationId = locationId
                    };
                    occupancyBuffer[i] = new EM_BufferElement_LocationOccupancy
                    {
                        Occupant = Entity.Null
                    };

                    if (locationDefinition == null)
                        continue;

                    int x = i % gridWidth;
                    int y = i / gridWidth;
                    float3 center = gridOrigin + new float3((x + 0.5f) * gridNodeSize, 0f, (y + 0.5f) * gridNodeSize);
                    Entity anchorEntity = CreateAdditionalEntity(TransformUsageFlags.WorldSpace);
                    LocalTransform anchorTransform = LocalTransform.FromPosition(center);

                    AddComponent(anchorEntity, anchorTransform);
                    AddComponent(anchorEntity, new EM_Component_LocationAnchor
                    {
                        Grid = gridEntity,
                        NodeIndex = i,
                        LocationId = locationId,
                        QueueRadius = locationDefinition.QueueRadius,
                        QueueSlotCount = locationDefinition.QueueSlotCount
                    });
                }
            }
        }
        #endregion
    }
}
