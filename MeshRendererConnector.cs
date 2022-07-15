using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnityNeos;

namespace Thundaga
{
    public class MeshRendererConnectorPacket : ConnectorPacket<MeshRendererConnector>
    {
        //todo: make meshrenderer and skinnedmeshrenderer packets use generics to avoid duplicating code
        //will require generic patching shenanigans
        private bool _isAssetAvailable;

        public MeshRendererConnectorPacket(MeshRendererConnector connector)
        {
            _connector = connector;
            _isAssetAvailable = connector.Owner.Mesh.IsAssetAvailable;
        }

        protected virtual void OnAttachRenderer()
        {
        }

        protected virtual void OnCleanupRenderer()
        {
        }
        
        public override void ApplyChange()
        {
            if (_isAssetAvailable)
            {
                if (_connector.MeshRenderer == null)
                {
                    var gameObject = MeshRendererConnectorPatches.get_attachedGameObject(_connector);
                    if (MeshRendererConnectorPatches.get_UseMeshFilter(_connector))
                        MeshRendererConnectorInfo.MeshFilter.SetValue(_connector, gameObject.AddComponent<MeshFilter>());
                    MeshRendererConnectorPatches.set_MeshRenderer(_connector, gameObject.AddComponent<MeshRenderer>());
                    OnAttachRenderer();
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