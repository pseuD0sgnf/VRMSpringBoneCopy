# VRMSpringBoneCopy
Make some modifications to VRMモデルのSpringBoneをコピーするツール(https://booth.pm/ja/items/1404532) for personal need, and translated into Chinese.

基于个人需要对VRMSpringBone复制工具进行少许修改，并将菜单翻译成中文。

# Notice
Please ensure that the positions and sizes of the source and target avatars are the same.

请保持来源与目标avatar位置和大小都相同。

If some colliders on the source avatar are located within objects that do not exist in the target avatar, do not select "Ignore missing colliders on the target object." Instead, these objects will be automatically copied.

如果来源avatar上的部分碰撞位于目标avatar中不存在的对象中，不要勾选“忽略目标对象不存在的碰撞”，会自动复制这些对象。

# Current Issues
The order of copying the above objects might be after the order of copying SpringBones, which is why clicking "Copy" twice is necessary to successfully copy SpringBoneColliders in SpringBones.

复制上述对象的顺序可能晚于复制SpringBone的顺序，因此要点击复制两次才能把SpringBone中的SpringBoneCollider成功复制进去。
