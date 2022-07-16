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
using Mesh = UnityEngine.Mesh;
using MeshRenderer = UnityEngine.MeshRenderer;
using Object = UnityEngine.Object;

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
        private int _materialCount;
        private int _materialPropertyBlockCount;
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
            _materialCount = _materials.Length;
            _materialPropertyBlocks = owner.MaterialPropertyBlocks.ToArray();
            _materialPropertyBlockCount = _materialPropertyBlocks.Length;
            
            if (!_isAssetAvailable) return;
            if (owner.SortingOrder.GetWasChangedAndClear()) _sortingOrder = owner.SortingOrder.Value;
            if (owner.ShadowCastMode.GetWasChangedAndClear())
                _shadowCastingMode = owner.ShadowCastMode.Value.ToUnity();
            if (owner.MotionVectorMode.GetWasChangedAndClear())
                _motionVectorGenerationMode = owner.MotionVectorMode.Value.ToUnity();
        }

        protected virtual void OnAttachRenderer()
        {
        }
        
        protected virtual void ChildApplyChange()
        {
        }

        protected virtual void OnCleanupRenderer()
        {
        }

        protected virtual void AssignMesh(Renderer renderer, Mesh mesh)
        {
        }
        public override void ApplyChange()
        {
            if (_isAssetAvailable)
            {
                var filter = MeshRendererConnectorPatches.get_UseMeshFilter(_connector);
                ChildApplyChange();
                var renderer = _connector.MeshRenderer;
                if (renderer == null)
                {
                    var gameObject = MeshRendererConnectorPatches.get_attachedGameObject(_connector);
                    if (filter)
                        MeshRendererConnectorInfo.MeshFilter.SetValue(_connector,
                            gameObject.AddComponent<MeshFilter>());
                    MeshRendererConnectorPatches.set_MeshRenderer(_connector, gameObject.AddComponent<MeshRenderer>());
                    OnAttachRenderer();
                }
                if (_meshWasChanged)
                {
                    var unity = _mesh.GetUnity();
                    if (filter) ((MeshFilter)MeshRendererConnectorInfo.MeshFilter.GetValue(_connector)).sharedMesh = unity;
                    else AssignMesh(renderer, unity);
                }
                var updatePropertyBlocksAnyway = false;
                if (_materialsChanged || _meshWasChanged)
                {
                    updatePropertyBlocksAnyway = true;
                    MeshRendererConnectorInfo.MaterialCount.SetValue(_connector, 1);
                    var nullMaterial = _isLocalElement
                        ? MaterialConnector.InvisibleMaterial
                        : MaterialConnector.NullMaterial;
                    var unityMaterials = (Material[])MeshRendererConnectorInfo.UnityMaterials.GetValue(_connector);
                    if (_materialCount > 1 || unityMaterials != null)
                    {
                        unityMaterials = unityMaterials.EnsureExactSize(_materialCount, allowZeroSize: true);
                        for (var i = 0; i < _materialCount; i++)
                        {
                            unityMaterials[i] = _materials[i]?.Asset.GetUnity(nullMaterial);
                        }
                        renderer.sharedMaterials = unityMaterials;
                        MeshRendererConnectorInfo.UnityMaterials.SetValue(_connector, unityMaterials);
                        MeshRendererConnectorInfo.MaterialCount.SetValue(_connector, _materialCount);
                    }
                    else
                    {
                        renderer.sharedMaterial = _materialCount == 1 ? _materials[0]?.Asset.GetUnity(nullMaterial) : nullMaterial;
                    }
                }
                if (_materialPropertyBlocksChanged || updatePropertyBlocksAnyway)
                {
                    for (var i = 0; i < _materialCount; i++)
                    {
                        renderer.SetPropertyBlock(
                            i < _materialPropertyBlockCount ? _materialPropertyBlocks[i]?.Asset.GetUnity() : null, i);
                    }
                }
                if (renderer.enabled != _enabled) renderer.enabled = _enabled;
                if (_sortingOrder.HasValue) renderer.sortingOrder = _sortingOrder.Value;
                if (_shadowCastingMode.HasValue) renderer.shadowCastingMode = _shadowCastingMode.Value;
                if (_motionVectorGenerationMode.HasValue)
                    renderer.motionVectorGenerationMode = _motionVectorGenerationMode.Value;
            }
            else CleanupRenderer(false);
        }

        private void CleanupRenderer(bool destroyingWorld)
        {
            if (!destroyingWorld && _connector.MeshRenderer != null && _connector.MeshRenderer.gameObject)
            {
                Object.Destroy(_connector.MeshRenderer);
                var filter = (MeshFilter)MeshRendererConnectorInfo.MeshFilter.GetValue(_connector);
                if (filter != null) Object.Destroy(filter);
            }
            OnCleanupRenderer();
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
    public class MeshRendererConnectorPatches
    {
        [HarmonyPatch("get_attachedGameObject")]
        [HarmonyReversePatch]
        public static GameObject get_attachedGameObject(MeshRendererConnector instance)
        {
            throw new NotImplementedException();
        }
        [HarmonyPatch("get_UseMeshFilter")]
        [HarmonyReversePatch]
        public static bool get_UseMeshFilter(MeshRendererConnector instance)
        {
            throw new NotImplementedException();
        }
        [HarmonyPatch("set_MeshRenderer")]
        [HarmonyReversePatch]
        public static void set_MeshRenderer(MeshRendererConnector instance, MeshRenderer value)
        {
            throw new NotImplementedException();
        }
    }
}