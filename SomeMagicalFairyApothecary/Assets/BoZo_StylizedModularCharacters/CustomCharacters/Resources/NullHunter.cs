using UnityEngine;
using UnityEditor;

public class NullFixer : EditorWindow
{
    [MenuItem("Tools/Auto-Fix Null Bones")]
    public static void FixNulls()
    {
        GameObject obj = Selection.activeGameObject;
        if (obj == null)
        {
            Debug.LogWarning("⚠️ Please select your character in the Hierarchy first!");
            return;
        }

        int fixedCount = 0;
        SkinnedMeshRenderer[] smrs = obj.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        
        foreach (var smr in smrs)
        {
            // Fix missing Root Bone
            if (smr.rootBone == null)
            {
                smr.rootBone = obj.transform; 
                Debug.Log($"Fixed missing Root Bone on {smr.gameObject.name}");
                fixedCount++;
            }

            // Fix empty slots in the Bones array
            if (smr.bones != null)
            {
                Transform[] currentBones = smr.bones;
                bool needsUpdate = false;
                
                for (int i = 0; i < currentBones.Length; i++)
                {
                    if (currentBones[i] == null)
                    {
                        // Plug the empty hole with the root bone so glTFast stops crashing
                        currentBones[i] = smr.rootBone; 
                        needsUpdate = true;
                        fixedCount++;
                    }
                }
                
                if (needsUpdate)
                {
                    smr.bones = currentBones;
                    EditorUtility.SetDirty(smr); // Tell Unity to save the changes
                    Debug.Log($"Fixed empty bone slots on {smr.gameObject.name}");
                }
            }
        }

        if (fixedCount > 0)
        {
            Debug.Log($"✅ SUCCESS: Automatically plugged {fixedCount} empty bone slots. Try exporting now!");
        }
        else
        {
            Debug.Log("No empty bone slots found. The issue might be an empty Material slot instead.");
        }
    }
}