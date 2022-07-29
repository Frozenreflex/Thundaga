/*
using System;
using System.Linq;
using System.Reflection;
using BaseX;
using FrooxEngine;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Rendering;
using UnityNeos;
using Material = UnityEngine.Material;
using SkinnedMeshRenderer = UnityEngine.SkinnedMeshRenderer;

namespace Thundaga
{
    public class SkinnedMeshRendererConnectorPacket : ConnectorPacket<SkinnedMeshRendererConnector>
    {
        private bool _isAssetAvailable;
        private bool _meshWasChanged;
        private bool _materialsChanged;
        private bool _materialPropertyBlocksChanged;
        private bool _isLocalElement;
        private bool _enabled;
        private FrooxEngine.Mesh _mesh;
        private IAssetProvider<FrooxEngine.Material>[] _materials;
        private IAssetProvider<FrooxEngine.MaterialPropertyBlock>[] _materialPropertyBlocks;
        private int? _sortingOrder;
        private ShadowCastingMode? _shadowCastingMode;
        private MotionVectorGenerationMode? _motionVectorGenerationMode;
        public SkinnedMeshRendererConnectorPacket(SkinnedMeshRendererConnector connector)
        {
            _connector = connector;
            var owner = connector.Owner;
            var mesh = owner.Mesh;
            _isAssetAvailable = mesh.IsAssetAvailable;
            _meshWasChanged = mesh.GetWasChangedAndClear();
            _materialsChanged = owner.MaterialsChanged;
            owner.MaterialsChanged = false;
            _materialPropertyBlocksChanged = owner.MaterialPropertyBlocksChanged;
            owner.MaterialPropertyBlocksChanged = false;
            _isLocalElement = owner.IsLocalElement;
            _mesh = mesh.Asset;
            _materials = owner.Materials.ToArray();
            _materialPropertyBlocks = owner.MaterialPropertyBlocks.ToArray();

            if (!_isAssetAvailable) return;
            if (owner.SortingOrder.GetWasChangedAndClear()) _sortingOrder = owner.SortingOrder.Value;
            if (owner.ShadowCastMode.GetWasChangedAndClear())
                _shadowCastingMode = owner.ShadowCastMode.Value.ToUnity();
            if (owner.MotionVectorMode.GetWasChangedAndClear())
                _motionVectorGenerationMode = owner.MotionVectorMode.Value.ToUnity();
        }
        public override void ApplyChange()
        {
            if (_isAssetAvailable)
            {
                var renderer = _connector.MeshRenderer;
                if (renderer == null)
                {
                    var go = new GameObject("");
                    var attached = SkinnedMeshRendererConnectorPatches.get_attachedGameObject(_connector);
                    go.transform.SetParent(attached.transform, false);
                    go.layer = attached.layer;
                    SkinnedMeshRendererConnectorPatches.set_SkinnedMeshRenderer(_connector, go.AddComponent<SkinnedMeshRenderer>());
                }
                if (_meshWasChanged) renderer.sharedMesh = _mesh.GetUnity();
                var updatePropertyBlocksAnyway = false;
                var count = _materials.Length;
                if (_materialsChanged || _meshWasChanged)
                {
                    updatePropertyBlocksAnyway = true;
                    SkinnedMeshRendererConnectorInfo.MaterialCount.SetValue(_connector, 1);
                    var nullMaterial = _isLocalElement
                        ? MaterialConnector.InvisibleMaterial
                        : MaterialConnector.NullMaterial;
                    var unityMaterials = (Material[])SkinnedMeshRendererConnectorInfo.UnityMaterials.GetValue(_connector);
                    if (count > 1 || unityMaterials != null)
                    {
                        unityMaterials = unityMaterials.EnsureExactSize(count, allowZeroSize: true);
                        for (var i = 0; i < count; i++)
                        {
                            unityMaterials[i] = _materials[i]?.Asset.GetUnity(nullMaterial);
                        }
                        renderer.sharedMaterials = unityMaterials;
                        SkinnedMeshRendererConnectorInfo.UnityMaterials.SetValue(_connector, unityMaterials);
                        SkinnedMeshRendererConnectorInfo.MaterialCount.SetValue(_connector, count);
                    }
                    else
                        renderer.sharedMaterial = count == 1
                            ? _materials[0]?.Asset.GetUnity(nullMaterial)
                            : nullMaterial;
                }
                if (_materialPropertyBlocksChanged || updatePropertyBlocksAnyway)
                    for (var i = 0; i < count; i++)
                        renderer.SetPropertyBlock(
                            i < _materialPropertyBlocks.Length ? _materialPropertyBlocks[i]?.Asset.GetUnity() : null, i);
                if (renderer.enabled != _enabled) 
                    renderer.enabled = _enabled;
                if (_sortingOrder.HasValue) 
                    renderer.sortingOrder = _sortingOrder.Value;
                if (_shadowCastingMode.HasValue) 
                    renderer.shadowCastingMode = _shadowCastingMode.Value;
                if (_motionVectorGenerationMode.HasValue)
                    renderer.motionVectorGenerationMode = _motionVectorGenerationMode.Value;
            }
            else SkinnedMeshRendererConnectorBasePatches.CleanupRenderer(_connector, false);
        }
    }
    public static class SkinnedMeshRendererConnectorInfo
    {
        public static readonly FieldInfo MeshFilter;
        public static readonly FieldInfo MaterialCount;
        public static readonly FieldInfo UnityMaterials;

        static SkinnedMeshRendererConnectorInfo()
        {
            MeshFilter = typeof(SkinnedMeshRendererConnector).GetField("meshFilter", AccessTools.all);
            MaterialCount = typeof(SkinnedMeshRendererConnector).GetField("materialCount", AccessTools.all);
            UnityMaterials = typeof(SkinnedMeshRendererConnector).GetField("unityMaterials", AccessTools.all);
        }
    }

    [HarmonyPatch(typeof(SkinnedMeshRendererConnector))]
    public static class SkinnedMeshRendererConnectorPatches
    {
        [HarmonyPatch("get_attachedGameObject")]
        [HarmonyReversePatch]
        public static GameObject get_attachedGameObject(SkinnedMeshRendererConnector instance) =>
            throw new NotImplementedException();

        [HarmonyPatch("set_SkinnedMeshRenderer")]
        [HarmonyReversePatch]
        public static void set_SkinnedMeshRenderer(SkinnedMeshRendererConnector instance, SkinnedMeshRenderer value) =>
            throw new NotImplementedException();
    }
    [HarmonyPatch(typeof(MeshRendererConnectorBase<FrooxEngine.SkinnedMeshRenderer, SkinnedMeshRenderer>))]
    public static class SkinnedMeshRendererConnectorBasePatches
    {
        [HarmonyPatch("CleanupRenderer")]
        [HarmonyReversePatch]
        public static void CleanupRenderer(MeshRendererConnectorBase<FrooxEngine.SkinnedMeshRenderer, SkinnedMeshRenderer> instance,
            bool value) => throw new NotImplementedException();
    }
}
*/