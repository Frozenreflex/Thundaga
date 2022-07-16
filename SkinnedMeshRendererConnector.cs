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
using Object = UnityEngine.Object;
using SkinnedMeshRenderer = UnityEngine.SkinnedMeshRenderer;

namespace Thundaga
{
    public class SkinnedMeshRendererConnectorPacket : ConnectorPacket<SkinnedMeshRendererConnector>
    {
        private bool _isAssetAvailable;
        public SkinnedMeshRendererConnectorPacket(SkinnedMeshRendererConnector connector)
        {
            _connector = connector;
            _isAssetAvailable = connector.Owner.Mesh.IsAssetAvailable;
        }
        public override void ApplyChange()
        {
            //attempting to weave between each and every private, internal, and protected var was too much for my brain
            //this should really only deviate from normal when rapidly driving the materials or the render methods,
            //but you really shouldn't be doing that anyway
            
            //this will apply the gameobject patches that i'm doing throughout that prevent the constant
            //gameobject creation and deletion, before going back to normal execution
            var renderer = _connector.MeshRenderer;
            if (renderer == null && _isAssetAvailable)
            {
                var gameObject = SkinnedMeshRendererConnectorPatches.get_attachedGameObject(_connector);
                SkinnedMeshRendererConnectorPatches.set_MeshRenderer(_connector, gameObject.AddComponent<SkinnedMeshRenderer>());
            }
            SkinnedMeshRendererConnectorPatches.ApplyChangesOriginal(_connector);
        }
    }
    public class SkinnedMeshRendererConnectorDestroyPacket : ConnectorPacket<SkinnedMeshRendererConnector>
    {
        private bool _destroyingWorld;
        public SkinnedMeshRendererConnectorDestroyPacket(SkinnedMeshRendererConnector connector, bool destroyingWorld)
        {
            _connector = connector;
            _destroyingWorld = destroyingWorld;
        }
        public override void ApplyChange()
        {
            SkinnedMeshRendererConnectorPatches.CleanupProxy(_connector);
            var bounds = (Object)SkinnedMeshRendererConnectorInfo.BoundsUpdater.GetValue(_connector);
            //this errors, but not having this is a potential memory leak
            //_connector.BoundsUpdated = null;
            if (bounds != null)
            {
                if (!_destroyingWorld && bounds)
                    Object.Destroy(bounds);
                SkinnedMeshRendererConnectorInfo.BoundsUpdater.SetValue(_connector, null);
            }
            SkinnedMeshRendererConnectorInfo.Bones.SetValue(_connector, null);
            
            CleanupRenderer(_destroyingWorld);
            SkinnedMeshRendererConnectorInfo.UnityMaterials.SetValue(_connector, null);
            SkinnedMeshRendererConnectorPatches.set_MeshRenderer(_connector, null);
        }
        public void CleanupRenderer(bool destroyingWorld)
        {
            if (destroyingWorld || _connector.MeshRenderer == null || !_connector.MeshRenderer.gameObject) return;
            Object.Destroy(_connector.MeshRenderer);
        }
    }
    public static class SkinnedMeshRendererConnectorInfo
    {
        public static readonly FieldInfo UnityMaterials;
        public static readonly FieldInfo BoundsUpdater;
        public static readonly FieldInfo Bones;
        public static readonly EventInfo BoundsUpdated;

        static SkinnedMeshRendererConnectorInfo()
        {
            UnityMaterials = typeof(SkinnedMeshRendererConnector).GetField("unityMaterials", AccessTools.all);
            BoundsUpdater = typeof(SkinnedMeshRendererConnector).GetField("_boundsUpdater", AccessTools.all);
            Bones = typeof(SkinnedMeshRendererConnector).GetField("bones", AccessTools.all);
            BoundsUpdated = typeof(SkinnedMeshRendererConnector).GetEvent("BoundsUpdated", AccessTools.all);
        }
    }
    [HarmonyPatch(typeof(SkinnedMeshRendererConnector))]
    public static class SkinnedMeshRendererConnectorPatches
    {
        [HarmonyPatch("ApplyChanges")]
        [HarmonyPrefix]
        private static bool ApplyChanges(SkinnedMeshRendererConnector __instance)
        {
            PacketManager.Enqueue(__instance.GetPacket());
            return false;
        }
        [HarmonyPatch("Destroy")]
        [HarmonyPrefix]
        private static bool Destroy(SkinnedMeshRendererConnector __instance, bool destroyingWorld)
        {
            SkinnedMeshRendererConnectorPatches.CleanupProxy(__instance);
            var bounds = (Object)SkinnedMeshRendererConnectorInfo.BoundsUpdater.GetValue(__instance);
            //this errors, but not having this is a potential memory leak
            //__instance.BoundsUpdated = null;
            if (bounds != null)
            {
                if (!destroyingWorld && bounds)
                    Object.Destroy(bounds);
                SkinnedMeshRendererConnectorInfo.BoundsUpdater.SetValue(__instance, null);
            }
            SkinnedMeshRendererConnectorInfo.Bones.SetValue(__instance, null);

            if (!destroyingWorld && __instance.MeshRenderer != null && __instance.MeshRenderer.gameObject)
                Object.Destroy(__instance.MeshRenderer);
            
            SkinnedMeshRendererConnectorInfo.UnityMaterials.SetValue(__instance, null);
            set_MeshRenderer(__instance, null);
            
            return false;
        }
        [HarmonyPatch("get_attachedGameObject")]
        [HarmonyReversePatch]
        public static GameObject get_attachedGameObject(SkinnedMeshRendererConnector instance)
        {
            throw new NotImplementedException();
        }
        //this executes the original version of ApplyChanges, not the patched version that only enqueues the packet
        [HarmonyPatch("ApplyChanges")]
        [HarmonyReversePatch]
        public static void ApplyChangesOriginal(SkinnedMeshRendererConnector instance)
        {
            throw new NotImplementedException();
        }
        [HarmonyPatch("set_MeshRenderer")]
        [HarmonyReversePatch]
        public static void set_MeshRenderer(SkinnedMeshRendererConnector instance, SkinnedMeshRenderer value)
        {
            throw new NotImplementedException();
        }
        [HarmonyPatch("CleanupProxy")]
        [HarmonyReversePatch]
        public static void CleanupProxy(SkinnedMeshRendererConnector instance)
        {
            throw new NotImplementedException();
        }
    }
    //since i have to access so many internal or private objects here, i'm going to try something else instead
    /*
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
        private int _materialCount;
        private int _materialPropertyBlockCount;
        private int? _sortingOrder;
        private ShadowCastingMode? _shadowCastingMode;
        private MotionVectorGenerationMode? _motionVectorGenerationMode;

        private bool _proxyBoundsSourceChanged;
        private bool _ExplicitLocalBoundsChanged;
        private bool _bonesChanged;
        private bool _blendshapeWeightsChanged;
        private SkinnedBounds _skinnedBounds;
        private BoundingBox _explicitLocalBounds;
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
            _materialCount = _materials.Length;
            _materialPropertyBlocks = owner.MaterialPropertyBlocks.ToArray();
            _materialPropertyBlockCount = _materialPropertyBlocks.Length;
            
            _skinnedBounds = owner.BoundsComputeMethod.Value;
            if (_skinnedBounds == SkinnedBounds.Static && owner.Slot.ActiveUserRoot == owner.LocalUserRoot)
                _skinnedBounds = SkinnedBounds.FastDisjointRootApproximate;
            if (_meshWasChanged)
            {
                _proxyBoundsSourceChanged = owner.ProxyBoundsSource.WasChanged;
                _ExplicitLocalBoundsChanged = owner.ExplicitLocalBounds.WasChanged;
                _bonesChanged = owner.BonesChanged;
                _blendshapeWeightsChanged = owner.BlendShapeWeightsChanged;
                owner.ProxyBoundsSource.WasChanged = false;
                owner.ExplicitLocalBounds.WasChanged = false;
                owner.BonesChanged = false;
                owner.BlendShapeWeightsChanged = false;
            }
            
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
                var renderer = _connector.MeshRenderer;
                if (renderer == null)
                {
                    var gameObject = SkinnedMeshRendererConnectorPatches.get_attachedGameObject(_connector);
                    SkinnedMeshRendererConnectorPatches.set_MeshRenderer(_connector, gameObject.AddComponent<SkinnedMeshRenderer>());
                    OnAttachRenderer();
                }
                if (_meshWasChanged) AssignMesh(renderer, _mesh.GetUnity());
                var updatePropertyBlocksAnyway = false;
                if (_materialsChanged || _meshWasChanged)
                {
                    updatePropertyBlocksAnyway = true;
                    SkinnedMeshRendererConnectorInfo.MaterialCount.SetValue(_connector, 1);
                    var nullMaterial = _isLocalElement
                        ? MaterialConnector.InvisibleMaterial
                        : MaterialConnector.NullMaterial;
                    var unityMaterials = (Material[])SkinnedMeshRendererConnectorInfo.UnityMaterials.GetValue(_connector);
                    if (_materialCount > 1 || unityMaterials != null)
                    {
                        unityMaterials = unityMaterials.EnsureExactSize(_materialCount, allowZeroSize: true);
                        for (var i = 0; i < _materialCount; i++)
                            unityMaterials[i] = _materials[i]?.Asset.GetUnity(nullMaterial);
                        renderer.sharedMaterials = unityMaterials;
                        SkinnedMeshRendererConnectorInfo.UnityMaterials.SetValue(_connector, unityMaterials);
                        SkinnedMeshRendererConnectorInfo.MaterialCount.SetValue(_connector, _materialCount);
                    }
                    else
                        renderer.sharedMaterial = _materialCount == 1
                            ? _materials[0]?.Asset.GetUnity(nullMaterial)
                            : nullMaterial;
                }
                if (_materialPropertyBlocksChanged || updatePropertyBlocksAnyway)
                    for (var i = 0; i < _materialCount; i++)
                        renderer.SetPropertyBlock(
                            i < _materialPropertyBlockCount ? _materialPropertyBlocks[i]?.Asset.GetUnity() : null, i);
                if (renderer.enabled != _enabled) renderer.enabled = _enabled;
                if (_sortingOrder.HasValue) renderer.sortingOrder = _sortingOrder.Value;
                if (_shadowCastingMode.HasValue) renderer.shadowCastingMode = _shadowCastingMode.Value;
                if (_motionVectorGenerationMode.HasValue)
                    renderer.motionVectorGenerationMode = _motionVectorGenerationMode.Value;

                if (_meshWasChanged || (SkinnedBounds)SkinnedMeshRendererConnectorInfo.CurrentBoundsMethod.GetValue(_connector) != _skinnedBounds ||
                    _proxyBoundsSourceChanged || _ExplicitLocalBoundsChanged)
                {
                    var skinBoundsUpdaterType = typeof(SkinnedMeshRendererConnector).Assembly.GetType("SkinBoundsUpdater");
                    var boundsUpdater = SkinnedMeshRendererConnectorInfo.BoundsUpdater.GetValue(_connector) as SkinBoundsUpdater;
                    if (_skinnedBounds != SkinnedBounds.Static && _skinnedBounds != SkinnedBounds.Proxy &&
                        _skinnedBounds != SkinnedBounds.Explicit)
                    {
                        if (boundsUpdater == null)
                        {
                            SkinnedMeshRendererConnectorPatches.set_LocalBoundingBoxAvailable(_connector, false);
                            boundsUpdater = _connector.MeshRenderer.gameObject.AddComponent<SkinBoundsUpdater>();
                            boundsUpdater.connector = _connector;
                        }
                        boundsUpdater.boundsMethod = _skinnedBounds;
                        boundsUpdater.boneMetadata = _mesh.BoneMetadata;
                        boundsUpdater.approximateBounds = _mesh.ApproximateBoneBounds;
                        _connector.MeshRenderer.updateWhenOffscreen = _skinnedBounds == SkinnedBounds.SlowRealtimeAccurate;
                    }
                    else
                    {
                        if (boundsUpdater != null)
                        {
                            SkinnedMeshRendererConnectorPatches.set_LocalBoundingBoxAvailable(_connector, false);
                            _connector.MeshRenderer.updateWhenOffscreen = false;
                            CleanupBoundsUpdater();
                        }
                        switch (_skinnedBounds)
                        {
                            case SkinnedBounds.Proxy:
                            {
                                CleanupProxy();
                                _connector._proxySource = _connector.Owner.ProxyBoundsSource.Target?.SkinConnector as SkinnedMeshRendererConnector;
                                if (_connector._proxySource != null)
                                {
                                    _connector._proxySource.BoundsUpdated += ProxyBoundsUpdated;
                                    ProxyBoundsUpdated();
                                }
                                break;
                            }
                            case SkinnedBounds.Explicit:
                                _connector.MeshRenderer.localBounds = _connector.Owner.ExplicitLocalBounds.Value.ToUnity();
                                SkinnedMeshRendererConnectorPatches.set_LocalBoundingBoxAvailable(_connector, true);
                                SendBoundsUpdated();
                                break;
                        }
                    }
                }
                if (_bonesChanged || _meshWasChanged)
                {
                    var num2 = _mesh?.Data?.BoneCount;
                    var num3 = _mesh?.Data?.BlendShapeCount;
                    var flag2 = num2 == 0 && num3 > 0;
                    if (flag2) num2 = 1;
                    _connector.bones = _connector.bones.EnsureExactSize(num2.GetValueOrDefault());
                    if (_connector.bones != null)
                    {
                        if (flag2)
                        {
                            _connector.bones[0] = _connector.attachedGameObject.transform;
                        }
                        else
                        {
                            var num4 = MathX.Min(_connector.bones.Length, _connector.Owner.Bones.Count);
                            var num5 = 0;
                            for (var i = 0; i < num4; i++)
                            {
                                var slotConnector = _connector.Owner.Bones[i]?.Connector as SlotConnector;
                                if (slotConnector == null) continue;
                                _connector.bones[i] = slotConnector.ForceGetGameObject().transform;
                                num5++;
                            }
                        }
                    }
                    _connector.MeshRenderer.bones = _connector.bones;
                    _connector.MeshRenderer.rootBone = flag2 ? _connector.attachedGameObject.transform : (_connector.Owner.GetRootBone()?.Connector as SlotConnector)?.ForceGetGameObject().transform;
                }
                if (_blendshapeWeightsChanged || _meshWasChanged)
                {
                    int valueOrDefault = (_connector.Owner.Mesh.Asset?.Data?.BlendShapeCount).GetValueOrDefault();
                    var j = 0;
                    SyncFieldList<float> blendShapeWeights = _connector.Owner.BlendShapeWeights;
                    for (var num6 = MathX.Min(valueOrDefault, blendShapeWeights.Count); j < num6; j++)
                    {
                        _connector.MeshRenderer.SetBlendShapeWeight(j, blendShapeWeights[j]);
                    }
                    for (; j < valueOrDefault; j++)
                    {
                        _connector.MeshRenderer.SetBlendShapeWeight(j, 0f);
                    }
                }
                SendBoundsUpdated();
            }
            else CleanupRenderer();
        }

        public void CleanupRenderer()
        {
            if (_connector.MeshRenderer == null || !_connector.MeshRenderer.gameObject) return;
            Object.Destroy(_connector.MeshRenderer);
            var filter = (MeshFilter)MeshRendererConnectorInfo.MeshFilter.GetValue(_connector);
            if (filter != null) Object.Destroy(filter);
        }
    }
    public class SkinnedMeshRendererConnectorDestroyPacket : ConnectorPacket<SkinnedMeshRendererConnector>
    {
        private bool _destroyingWorld;
        public SkinnedMeshRendererConnectorDestroyPacket(SkinnedMeshRendererConnector connector, bool destroyingWorld)
        {
            _connector = connector;
            _destroyingWorld = destroyingWorld;
        }
        public override void ApplyChange()
        {
            CleanupRenderer(_destroyingWorld);
            SkinnedMeshRendererConnectorInfo.UnityMaterials.SetValue(_connector, null);
            SkinnedMeshRendererConnectorInfo.MeshFilter.SetValue(_connector, null);
            SkinnedMeshRendererConnectorPatches.set_MeshRenderer(_connector, null);
        }
        public void CleanupRenderer(bool destroyingWorld)
        {
            if (destroyingWorld || _connector.MeshRenderer == null || !_connector.MeshRenderer.gameObject) return;
            Object.Destroy(_connector.MeshRenderer);
            var filter = (MeshFilter)MeshRendererConnectorInfo.MeshFilter.GetValue(_connector);
            if (filter != null) Object.Destroy(filter);
        }
    }
    public static class SkinnedMeshRendererConnectorInfo
    {
        public static readonly FieldInfo MeshFilter;
        public static readonly FieldInfo MaterialCount;
        public static readonly FieldInfo UnityMaterials;
        public static readonly FieldInfo CurrentBoundsMethod;
        public static readonly FieldInfo BoundsUpdater;

        static SkinnedMeshRendererConnectorInfo()
        {
            MeshFilter = typeof(MeshRendererConnector).GetField("meshFilter", AccessTools.all);
            MaterialCount = typeof(MeshRendererConnector).GetField("materialCount", AccessTools.all);
            UnityMaterials = typeof(MeshRendererConnector).GetField("unityMaterials", AccessTools.all);
            CurrentBoundsMethod = typeof(MeshRendererConnector).GetField("_currentBoundsMethod", AccessTools.all);
            BoundsUpdater = typeof(MeshRendererConnector).GetField("_boundsUpdater", AccessTools.all);
        }
    }

    [HarmonyPatch(typeof(SkinnedMeshRendererConnector))]
    public class SkinnedMeshRendererConnectorPatches
    {
        [HarmonyPatch("get_attachedGameObject")]
        [HarmonyReversePatch]
        public static GameObject get_attachedGameObject(SkinnedMeshRendererConnector instance)
        {
            throw new NotImplementedException();
        }
        [HarmonyPatch("set_MeshRenderer")]
        [HarmonyReversePatch]
        public static void set_MeshRenderer(SkinnedMeshRendererConnector instance, SkinnedMeshRenderer value)
        {
            throw new NotImplementedException();
        }
        [HarmonyPatch("set_LocalBoundingBoxAvailable")]
        [HarmonyReversePatch]
        public static void set_LocalBoundingBoxAvailable(SkinnedMeshRendererConnector instance, bool value)
        {
            throw new NotImplementedException();
        }
    }
    */
}