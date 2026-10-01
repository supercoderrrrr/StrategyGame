using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitRagdoll : MonoBehaviour
{
    [SerializeField] private Transform ragdollRootBone;

    public void Setup(Transform originalRootBone, Vector3 explosionSourcePosition = default, float explosionForce = 0, float explosionRange = 0)
    {
        MatchAllChildTransforms(originalRootBone, ragdollRootBone);

        if (explosionForce > 0)
        {
            //Bomb explosion
            ApplyExplosionToRagdoll(ragdollRootBone, explosionForce, explosionSourcePosition, explosionRange);
        }
        else
        {
            Vector3 randomDir = new Vector3(Random.Range(-1f, +1f), 0, Random.Range(-1f, +1f));
            ApplyExplosionToRagdoll(ragdollRootBone, 300f, transform.position + randomDir, 10f);
        }
    }

    private void MatchAllChildTransforms(Transform originalRoot, Transform cloneRoot)
    {
        Transform[] originalBones = originalRoot.GetComponentsInChildren<Transform>();
        Transform[] cloneBones = cloneRoot.GetComponentsInChildren<Transform>();

        Dictionary<string, Transform> originalBoneDict = new Dictionary<string, Transform>();

        foreach (Transform bone in originalBones)
        {
            if (!originalBoneDict.ContainsKey(bone.name))
            {
                originalBoneDict.Add(bone.name, bone);
            }
        }

        foreach (Transform cloneBone in cloneBones)
        {
            if (originalBoneDict.TryGetValue(cloneBone.name, out Transform originalBone))
            {
                cloneBone.position = originalBone.position;
                cloneBone.rotation = originalBone.rotation;
            }
        }
    }

    private void ApplyExplosionToRagdoll(Transform root, float explosionForce, Vector3 explosionPosition, float explosionRange)
    {
        Rigidbody[] rigidbodies = root.GetComponentsInChildren<Rigidbody>();

        foreach (Rigidbody rb in rigidbodies)
        {
            if (explosionForce > 0)
            {
                rb.AddExplosionForce(explosionForce, explosionPosition, explosionRange);
            }
            else
            {
                Vector3 randomDir = new Vector3(Random.Range(-1f, +1f), 0, Random.Range(-1f, +1f));
                rb.AddExplosionForce(300f, transform.position + randomDir, 10f);
            }
        }
    }

    /*private void MatchAllChildTransforms(Transform root, Transform clone)
    {
        foreach(Transform child in root)
        {
            Transform cloneChild = clone.Find(child.name);
            if (cloneChild!=null)
            {
                cloneChild.position = child.position;
                cloneChild.rotation = child.rotation;

                MatchAllChildTransforms (child, cloneChild);
            }
        }

    }

    private void ApplyExplosionToRagdoll(Transform root, float explosionForce, Vector3 explosionPosition, float explosionRange)
    {
        foreach(Transform child in root)
        {
            if(child.TryGetComponent<Rigidbody>(out Rigidbody childRigidbody)){
                childRigidbody.AddExplosionForce(explosionForce, explosionPosition, explosionRange);
            }

            ApplyExplosionToRagdoll(child, explosionForce, explosionPosition, explosionRange);
        }
    }*/
}
