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
using MeshRenderer = UnityEngine.MeshRenderer;

namespace Thundaga
{
    public class MeshRendererConnectorPacket : ConnectorPacket<MeshRendererConnector>
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
        public MeshRendererConnectorPacket(MeshRendererConnector connector)
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
                    var attached = MeshRendererConnectorPatches.get_attachedGameObject(_connector);
                    go.transform.SetParent(attached.transform, false);
                    go.layer = attached.layer;
                    MeshRendererConnectorInfo.MeshFilter.SetValue(_connector,
                        go.AddComponent<MeshFilter>());
                    MeshRendererConnectorPatches.set_MeshRenderer(_connector, go.AddComponent<MeshRenderer>());
                }
                if (_meshWasChanged)
                    ((MeshFilter) MeshRendererConnectorInfo.MeshFilter.GetValue(_connector)).sharedMesh =
                        _mesh.GetUnity();
                var updatePropertyBlocksAnyway = false;
                var count = _materials.Length;
                if (_materialsChanged || _meshWasChanged)
                {
                    updatePropertyBlocksAnyway = true;
                    MeshRendererConnectorInfo.MaterialCount.SetValue(_connector, 1);
                    var nullMaterial = _isLocalElement
                        ? MaterialConnector.InvisibleMaterial
                        : MaterialConnector.NullMaterial;
                    var unityMaterials = (Material[])MeshRendererConnectorInfo.UnityMaterials.GetValue(_connector);
                    if (count > 1 || unityMaterials != null)
                    {
                        unityMaterials = unityMaterials.EnsureExactSize(count, allowZeroSize: true);
                        for (var i = 0; i < count; i++)
                        {
                            unityMaterials[i] = _materials[i]?.Asset.GetUnity(nullMaterial);
                        }
                        renderer.sharedMaterials = unityMaterials;
                        MeshRendererConnectorInfo.UnityMaterials.SetValue(_connector, unityMaterials);
                        MeshRendererConnectorInfo.MaterialCount.SetValue(_connector, count);
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
            else MeshRendererConnectorBasePatches.CleanupRenderer(_connector, false);
        }
    }
    public static class MeshRendererConnectorInfo
    {
        public static readonly FieldInfo MeshFilter;
        public static readonly FieldInfo MaterialCount;
        public static readonly FieldInfo UnityMaterials;

        static MeshRendererConnectorInfo()
        {
            MeshFilter = typeof(MeshRendererConnector).GetField("meshFilter", AccessTools.all);
            MaterialCount = typeof(MeshRendererConnector).GetField("materialCount", AccessTools.all);
            UnityMaterials = typeof(MeshRendererConnector).GetField("unityMaterials", AccessTools.all);
        }
    }

    [HarmonyPatch(typeof(MeshRendererConnector))]
    public static class MeshRendererConnectorPatches
    {
        [HarmonyPatch("get_attachedGameObject")]
        [HarmonyReversePatch]
        public static GameObject get_attachedGameObject(MeshRendererConnector instance) =>
            throw new NotImplementedException();

        [HarmonyPatch("set_MeshRenderer")]
        [HarmonyReversePatch]
        public static void set_MeshRenderer(MeshRendererConnector instance, MeshRenderer value) =>
            throw new NotImplementedException();
    }
    [HarmonyPatch(typeof(MeshRendererConnectorBase<FrooxEngine.MeshRenderer, MeshRenderer>))]
    public static class MeshRendererConnectorBasePatches
    {
        [HarmonyPatch("CleanupRenderer")]
        [HarmonyReversePatch]
        public static void CleanupRenderer(MeshRendererConnectorBase<FrooxEngine.MeshRenderer, MeshRenderer> instance,
            bool value) => throw new NotImplementedException();
    }
}
*/