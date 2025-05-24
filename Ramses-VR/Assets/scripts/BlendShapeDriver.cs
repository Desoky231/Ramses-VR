using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class BlendShapeDriver : MonoBehaviour
{
    public SkinnedMeshRenderer masterRenderer;
    [System.Serializable]
    public class BlendShapeMapping
    {
        public string masterBlendShapeName;

        [System.Serializable]
        public class SlaveBlendShape
        {
            public SkinnedMeshRenderer targetRenderer;
        }

        public List<SlaveBlendShape> slaves = new List<SlaveBlendShape>();
    }

    public List<BlendShapeMapping> mappings = new List<BlendShapeMapping>();

    void Update()
    {
        if (!Application.isPlaying)
        {
#if UNITY_EDITOR
            UnityEditor.SceneView.RepaintAll(); // editörde blend shape'ler anlýk görünsün
#endif
        }

        ApplyBlendShapes();
    }

    void ApplyBlendShapes()
    {
        foreach (var mapping in mappings)
        {
            if (masterRenderer == null) continue;

            var masterMesh = masterRenderer.sharedMesh;
            if (masterMesh == null) continue;

            int masterIndex = masterMesh.GetBlendShapeIndex(mapping.masterBlendShapeName);
            if (masterIndex == -1) continue;

            float masterWeight = masterRenderer.GetBlendShapeWeight(masterIndex);

            foreach (var slave in mapping.slaves)
            {
                if (slave.targetRenderer == null) continue;

                var targetMesh = slave.targetRenderer.sharedMesh;
                if (targetMesh == null) continue;

                int targetIndex = targetMesh.GetBlendShapeIndex(mapping.masterBlendShapeName);
                if (targetIndex == -1) continue;

                slave.targetRenderer.SetBlendShapeWeight(targetIndex, masterWeight);
            }
        }
    }
}
