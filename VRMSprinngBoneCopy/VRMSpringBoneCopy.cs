using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Reflection;

namespace VRM
{
    public class VRMSpringBoneCopy : EditorWindow
    {

        [MenuItem("VRM/Spring Bone Copy")]
        static void Open()
        {
            GetWindow<VRMSpringBoneCopy>();
        }

        GameObject srcGo;

        GameObject dstGo;

        bool ignoreMissingColliders = true; // Default is ignore

        /// <summary>
        /// OnGUI is called for rendering and handling GUI events.
        /// This function can be called multiple times per frame (one call per event).
        /// </summary>
        void OnGUI()
        {
            EditorGUILayout.LabelField("复制来源GameObject");
            this.srcGo = EditorGUILayout.ObjectField(this.srcGo, typeof(GameObject), true) as GameObject;
            EditorGUILayout.LabelField("复制目标GameObject");
            this.dstGo = EditorGUILayout.ObjectField(this.dstGo, typeof(GameObject), true) as GameObject;

            // 添加一个 Toggle 选项来选择是否忽略不存在的碰撞器
            ignoreMissingColliders = EditorGUILayout.Toggle("忽略目标对象不存在的碰撞", ignoreMissingColliders);


            if (GUILayout.Button("复制"))
            {
                copyComponents();
                showResult();
            }
        }

        Transform[] transformList;

        List<string> colliderTargetError = new List<string>();
        List<string> springTargetBone = new List<string>();

        List<string> springBoneError = new List<string>();

        void copyComponents()
        {
            this.colliderTargetError.Clear();
            this.springTargetBone.Clear();
            this.springBoneError.Clear();

            deleteComponents();

            this.transformList = this.dstGo.GetComponentsInChildren<Transform>();

            var colliders = this.srcGo.GetComponentsInChildren<VRMSpringBoneColliderGroup>();
            foreach (var c in colliders)
            {
                var targetTransform = this.transformList.FirstOrDefault(x => x.name == c.transform.name);
                if (targetTransform != null)
                {
                    var col = targetTransform.gameObject.AddComponent<VRMSpringBoneColliderGroup>();
                    col.Colliders = new VRMSpringBoneColliderGroup.SphereCollider[c.Colliders.Length];
                    copyField(c, col, "m_gizmoColor");
                    for (int i = 0; i < col.Colliders.Length; i++)
                    {
                        col.Colliders[i] = new VRMSpringBoneColliderGroup.SphereCollider();
                        col.Colliders[i].Offset = c.Colliders[i].Offset;
                        col.Colliders[i].Radius = c.Colliders[i].Radius;
                    }
                }
                else if(!ignoreMissingColliders) // Not ignoring missing colliders
                {
                    // Find the parent of the source transform in the target hierarchy
                    var parentTransformInTarget = FindParentTransformInTarget(c.transform);

                    // Create new GameObject with matching name and copy collider group
                    var newGameObject = new GameObject(c.transform.name);
                    var newTransform = newGameObject.transform; // Store the new Transform reference
                    var col = newGameObject.AddComponent<VRMSpringBoneColliderGroup>();
                    col.Colliders = new VRMSpringBoneColliderGroup.SphereCollider[c.Colliders.Length];
                    copyField(c, col, "m_gizmoColor");
                    for (int i = 0; i < col.Colliders.Length; i++)
                    {
                        col.Colliders[i] = new VRMSpringBoneColliderGroup.SphereCollider();
                        col.Colliders[i].Offset = c.Colliders[i].Offset;
                        col.Colliders[i].Radius = c.Colliders[i].Radius;
                    }

                    // Attach the new GameObject to the parent transform
                    newTransform.transform.SetParent(parentTransformInTarget);

                    // Set the position of the new Transform to match the source Transform
                    newTransform.position = c.transform.position;
                }
                else
                {
                    Debug.LogWarning("Not found VRMSpringBoneColliderGroup bone->" + c.gameObject.name);
                    this.colliderTargetError.Add(c.gameObject.name);
                }
            }

            var springbone = this.srcGo.GetComponentsInChildren<VRMSpringBone>();
            foreach (var c in springbone)
            {
                var targetTransform = this.transformList.FirstOrDefault(x => x.name == c.transform.name);
                if (targetTransform != null)
                {
                    var spring = targetTransform.gameObject.AddComponent<VRMSpringBone>();
                    spring.m_comment = c.m_comment;
                    copyField(c, spring, "m_drawGizmo");
                    copyField(c, spring, "m_gizmoColor");
                    spring.m_stiffnessForce = c.m_stiffnessForce;
                    spring.m_gravityPower = c.m_gravityPower;
                    spring.m_gravityDir = c.m_gravityDir;
                    spring.m_dragForce = c.m_dragForce;
                    spring.m_center = searchTranform(c.m_center);
                    foreach (var b in c.RootBones)
                    {
                        var bb = searchTranform(b);
                        if (bb != null)
                        {
                            spring.RootBones.Add(bb);
                        }
                        else
                        {
                            this.springTargetBone.Add(b.name);
                        }
                    }
                    spring.m_hitRadius = c.m_hitRadius;
                    if (c.ColliderGroups.Length > 0)
                    {
                        spring.ColliderGroups = new VRMSpringBoneColliderGroup[c.ColliderGroups.Length];
                        for (int i = 0; i < spring.ColliderGroups.Length; i++)
                        {
                            var t = searchTranform(c.ColliderGroups[i].transform);
                            if (t != null)
                            {
                                spring.ColliderGroups[i] = t.GetComponent<VRMSpringBoneColliderGroup>();
                            }
                            else
                            {
                                Debug.LogWarning("Not found VRMSpringBoneColliderGroup ->" + c.ColliderGroups[i].transform.name);
                                this.springTargetBone.Add(c.ColliderGroups[i].transform.name);
                            }
                        }
                    }

                }
                else
                {
                    Debug.LogWarning("Not found VRMSpringBone ->" + c.gameObject.name);
                    this.springBoneError.Add(c.gameObject.name);
                }
            }
        }

        void showResult()
        {
            if (this.colliderTargetError.Count == 0 &&
                this.springTargetBone.Count == 0 &&
                this.springBoneError.Count == 0)
            {
                EditorUtility.DisplayDialog("复制完成", "成功复制所有内容", "OK");
            }
            else
            {
                string message = "无法复制部分组件\n";
                if (this.colliderTargetError.Count > 0)
                {
                    message += "找不到VRMSpringBoneColliderGroup的骨骼\n";
                    foreach (var e in this.colliderTargetError)
                    {
                        message += e + "\n";
                    }
                }

                if (this.springBoneError.Count > 0)
                {
                    message += "找不到添加VRMSpringBone的位置\n";
                    foreach (var e in this.springBoneError)
                    {
                        message += e + "\n";
                    }
                }

                if (this.springTargetBone.Count > 0)
                {
                    message += "无法找到动骨\n";
                    foreach (var e in this.springTargetBone)
                    {
                        message += e + "\n";
                    }
                }

                EditorUtility.DisplayDialog("复制完成", message, "OK");
            }
        }

        Transform FindParentTransformInTarget(Transform sourceTransform)
        {
            var sourceParentTransform = sourceTransform.parent;

            foreach (var targetTransform in transformList)
            {
                if (targetTransform.name == sourceParentTransform.name)
                {
                    return targetTransform;
                }
            }

            return null;
        }

        Transform searchTranform(Transform t)
        {
            if (t == null)
            {
                return null;
            }

            var value = this.transformList.FirstOrDefault(x => x.name == t.name);

            return value;
        }

        void deleteComponents()
        {
            var cols = this.dstGo.GetComponentsInChildren<VRMSpringBoneColliderGroup>();
            foreach (var c in cols)
            {
                DestroyImmediate(c);
            }

            var springs = this.dstGo.GetComponentsInChildren<VRMSpringBone>();
            foreach (var s in springs)
            {
                DestroyImmediate(s);
            }
        }

        // private変数のコピー
        void copyField(object src, object dst, string fieldName)
        {
            var srcField = src.GetType().GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic |
                                    BindingFlags.Instance | BindingFlags.Static |
                                    BindingFlags.DeclaredOnly);

            if (srcField == null)
            {
                Debug.LogWarning("srcField is null");
                return;
            }

            var dstField = dst.GetType().GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic |
                                    BindingFlags.Instance | BindingFlags.Static |
                                    BindingFlags.DeclaredOnly);

            if (dstField == null)
            {
                Debug.LogWarning("dstField is null");
                return;
            }

            dstField.SetValue(dst, srcField.GetValue(src));
        }
    }

}