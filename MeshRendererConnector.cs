using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnityNeos;

namespace Thundaga
{
    public class MeshRendererConnectorPacket : ConnectorPacket<MeshRendererConnector>
    {
        private bool _isAssetAvailable;
        private bool _meshWasChanged;
        private FrooxEngine.Mesh _mesh;

        public MeshRendererConnectorPacket(MeshRendererConnector connector)
        {
            _connector = connector;
            var owner = connector.Owner;
            var mesh = owner.Mesh;
            _isAssetAvailable = mesh.IsAssetAvailable;
            _meshWasChanged = mesh.GetWasChangedAndClear();
            _mesh = mesh.Asset;
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
                if (_connector.MeshRenderer == null)
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
                    else AssignMesh(_connector.MeshRenderer, unity);
                }
            }
            else
            {
                
            }
        }
    }
    
    public static class MeshRendererConnectorInfo
    {
        public static readonly FieldInfo MeshFilter;

        static MeshRendererConnectorInfo()
        {
            MeshFilter = typeof(MeshRendererConnector).GetField("meshFilter", AccessTools.all);
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