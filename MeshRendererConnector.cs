using System;
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
                    var gameObject = new GameObject("");
                    var go = MeshRendererConnectorPatches.get_attachedGameObject(_connector);
                    gameObject.transform.SetParent(go.transform, false);
                    gameObject.layer = go.layer;
                    if (MeshRendererConnectorPatches.get_UseMeshFilter(_connector))
                        //_connector.meshFilter = gameObject.AddComponent<MeshFilter>();
                    MeshRendererConnectorPatches.set_MeshRenderer(_connector, gameObject.AddComponent<MeshRenderer>());
                    OnAttachRenderer();
                }
            }
            else
            {
                
            }
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