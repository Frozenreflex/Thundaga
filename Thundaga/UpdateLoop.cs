using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using BaseX;
using FrooxEngine;
using HarmonyLib;
using UnityEngine;
using UnityEngine.XR;
using UnityNeos;
using System.Threading;
using System.Threading.Tasks;
using Debug = UnityEngine.Debug;
using ParticleSystem = FrooxEngine.ParticleSystem;
using ThreadPriority = System.Threading.ThreadPriority;

namespace Thundaga
{
    public class UpdateLoop
    {
        public static bool Shutdown;
        public static float TickRate = 60;

        public static void Update()
        {
            var dateTime = DateTime.UtcNow;
            var tickRate = 1f / TickRate;
            while (!Shutdown)
            {
                Engine.Current.RunUpdateLoop();
                PacketManager.FinishNeosQueue();
                dateTime = dateTime.AddTicks((long) (10000000.0 * tickRate));
                var timeSpan = dateTime - DateTime.UtcNow;
                if (timeSpan.TotalMilliseconds > 0.0)
                {
                    var totalMilliseconds = (int) timeSpan.TotalMilliseconds;
                    if (totalMilliseconds > 0)
                        Thread.Sleep(totalMilliseconds);
                }
                else
                    dateTime = DateTime.UtcNow;
            }
        }
    }

    [HarmonyPatch(typeof(FrooxEngineRunner))]
    public static class FrooxEngineRunnerPatch
    {
        public static List<IConnector> Connectors = new List<IConnector>();
        private static bool _startedUpdating;
        private static int _lastDiagnosticReport = 1800;
        private static IntPtr? _renderThreadPointer;
        public static bool ShouldRefreshAllConnectors;
        private static readonly FieldInfo LocalSlots = typeof(World).GetField("_localSlots", AccessTools.all);
        public static ThreadPriority NeosThreadPriority = ThreadPriority.Normal;
        public static int AutoLocalRefreshTick = 1800;
        private static int _autoLocalRefreshTicks;

        private static void RefreshAllConnectors()
        {
            CheckForNullConnectors();
            var count = Engine.Current.WorldManager.Worlds.Sum(RefreshConnectorsForWorld);
            UniLog.Log($"Refreshed {count} components");
            //prevent updating removed connectors
            PacketManager.IntermittentPacketQueue.Clear();
            PacketManager.NeosPacketQueue.Clear();
        }
        private static void RefreshAllLocalConnectors() =>
            Engine.Current.WorldManager.Worlds.Sum(RefreshLocalConnectorsForWorld);

        private static int RefreshConnectorsForWorld(World world) => RefreshConnectorsForWorld(world, false);
        private static int RefreshLocalConnectorsForWorld(World world) => RefreshConnectorsForWorld(world, true);
        private static int RefreshConnectorsForWorld(World world, bool localOnly)
        {
            //refresh world focus to fix overlapping worlds
            var focus = (World.WorldFocus)WorldConnectorPatch.Focus.GetValue(world);
            if (focus == World.WorldFocus.Focused || focus == World.WorldFocus.Background)
            {
                var connector = (WorldConnector) world.Connector;
                if (connector?.WorldRoot != null) connector.WorldRoot.SetActive(focus == World.WorldFocus.Focused);
            }
            //since the world state is constantly shifting we have to encapsulate them with try catch to prevent crashes
            var count = 0;
            try
            {
                if (!localOnly)
                {
                    var slots = world.AllSlots.ToList();
                    foreach (var slot in slots)
                    {
                        if (slot == null) continue;
                        var components = slot.Components.ToList();
                        foreach (var component in components)
                        {
                            if (!(component is ImplementableComponent<IConnector> implementable) || implementable is ParticleSystem) continue;
                            RefreshConnector(implementable);
                            count++;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                UniLog.Log(e);
            }
            try
            {
                var locals = ((List<Slot>) LocalSlots.GetValue(world)).ToList();
                foreach (var slot in locals)
                {
                    var components = slot.Components.ToList();
                    foreach (var component in components)
                    {
                        if (!(component is ImplementableComponent<IConnector> implementable) || implementable is ParticleSystem) continue;
                        RefreshConnector(implementable);
                        count++;
                    }
                }
            }
            catch (Exception e)
            {
                UniLog.Log(e);
            }
            return count;
        }

        private static void CheckForNullConnectors()
        {
            var toRemove = new List<IConnector>();
            foreach (var connector in Connectors)
            {
                if (connector.Owner != null && !connector.Owner.IsDestroyed && !connector.Owner.IsRemoved) continue;
                try
                {
                    connector.Destroy(false);
                    connector.RemoveOwner();
                    toRemove.Add(connector);
                }
                catch (Exception e)
                {
                    UniLog.Log(e);
                }
            }
            foreach (var remove in toRemove) Connectors.Remove(remove);
        }

        private static void RefreshConnector(ImplementableComponent<IConnector> implementable)
        {
            try
            {
                implementable.Connector.Destroy(false);
                ImplementableComponentPatches.InitializeConnector(implementable);
                implementable.Connector.AssignOwner(implementable);
                var con = implementable.Connector;
                con.Initialize();
                if (con is MeshRendererConnector mesh)
                {
                    MeshRendererConnectorPatch.set_meshWasChanged(mesh, true);
                }
                if (con is SkinnedMeshRendererConnector smesh)
                {
                    SkinnedMeshRendererConnectorPatchB.set_meshWasChanged(smesh, true);
                }
                con.ApplyChanges();
            }
            catch (Exception e)
            {
                UniLog.Log(e);
            }
        }

        [HarmonyPatch("Update")]
        [HarmonyPrefix]
        private static bool Update(FrooxEngineRunner __instance, ref bool ___engineInitialized,
            ref bool ___shutdownEnvironment, ref Engine ___engine, ref Stopwatch ___stopwatch,
            ref Stopwatch ____externalStopwatch, ref SystemInfoConnector ___systemInfoConnector,
            ref Stopwatch ____framerateStopwatch, ref int ____framerateCounter, ref SpinQueue<Action> ___actions,
            ref bool? ____lastVRactive, ref List<World> ____worlds, ref World ____lastFocusedWorld,
            ref bool ___updateDynamicGI, ref bool ___updateDynamicGIimmediate, ref float ___lastDynamicGIupdate,
            ref AudioListener ___audioListener, ref HeadOutput ___vrOutputRoot, ref HeadOutput ___screenOutputRoot)
        {
            if (!___engineInitialized)
                return false;
            UpdateLoop.Shutdown = ___shutdownEnvironment;
            if (___shutdownEnvironment)
            {
                UniLog.Log("Shutting down environment");
                try
                {
                    ___engine?.Dispose();
                }
                catch (Exception ex)
                {
                    UniLog.Error("Exception disposing the engine:\n" + ___engine);
                }
                ___engine = null;
                QuitApplication(__instance);
                return false;
            }
            if (!_startedUpdating)
            {
                var updateLoop = new Thread(UpdateLoop.Update)
                {
                    Name = "Update Loop",
                    Priority = NeosThreadPriority,
                    IsBackground = false
                };
                updateLoop.Start();
                _startedUpdating = true;
            }
            else
            {
                _lastDiagnosticReport--;
                if (_lastDiagnosticReport <= 0)
                {
                    
                    _lastDiagnosticReport = 3600;
                    //_refreshAllConnectors = true;
                    //UniLog.Log("Reinitializing...");
                    //UniLog.Log("SkinnedMeshRenderer: " + UnityEngine.Object.FindObjectsOfType<UnityEngine.SkinnedMeshRenderer>().Length);
                }

                ___stopwatch.Restart();
                ____externalStopwatch.Stop();
                try
                {
                    if (___engine != null)
                    {
                        SystemInfoConnectorPatch.ExternalUpdateTime.SetValue(___systemInfoConnector,
                            ____externalStopwatch.ElapsedMilliseconds * (1f / 1000f));
                        if (!____framerateStopwatch.IsRunning)
                        {
                            ____framerateStopwatch.Restart();
                        }
                        else
                        {
                            ++____framerateCounter;
                            var elapsedMilliseconds = ____framerateStopwatch.ElapsedMilliseconds;
                            if (elapsedMilliseconds >= 500L)
                            {
                                if (___systemInfoConnector != null)
                                    SystemInfoConnectorPatch.FPS.SetValue(___systemInfoConnector, ____framerateCounter /
                                        (elapsedMilliseconds * (1f / 1000f)));
                                ____framerateCounter = 0;
                                ____framerateStopwatch.Restart();
                            }
                        }

                        if (___systemInfoConnector != null)
                        {
                            SystemInfoConnectorPatch.RenderTime.SetValue(___systemInfoConnector,
                                !XRStats.TryGetGPUTimeLastFrame(out var gpuTimeLastFrame) ? -1f
                                    : gpuTimeLastFrame * (1f / 1000f));
                            SystemInfoConnectorPatch.ImmediateFPS.SetValue(___systemInfoConnector,
                                1f / Time.unscaledDeltaTime);
                        }
                        ___engine.InputInterface.UpdateWindowResolution(new int2(Screen.width,
                            Screen.height));

                        var mouse = UnityEngine.InputSystem.Mouse.current;
                        if (mouse != null) MouseDriverPatch.NewDirectDelta += mouse.delta.ReadValue().ToNeos();

                        //___engine.RunUpdateLoop(6.0);
                        var packets = PacketManager.GetQueuedPackets();
                        foreach (var packet in packets)
                        {
                            try
                            {
                                packet.ApplyChange();
                            }
                            catch (Exception e)
                            {
                                UniLog.Error(e.ToString());
                            }
                        }
                        var assetTaskQueue = PacketManager.GetQueuedAssetTasks();
                        foreach (var task in assetTaskQueue)
                        {
                            try
                            {
                                task();
                            }
                            catch (Exception e)
                            {
                                UniLog.Error(e.ToString());
                            }
                        }
                        var assetIntegrator = Engine.Current.AssetManager.Connector as UnityAssetIntegrator;
                        if (!_renderThreadPointer.HasValue)
                            _renderThreadPointer =
                                (IntPtr) AssetIntegratorPatch.RenderThreadPointer.GetValue(assetIntegrator);
                        /*
                        if (((SpinQueue<>) AssetIntegratorPatch.RenderThreadQueue
                                .GetValue(assetIntegrator))
                            .Count > 0)
                            */
                        GL.IssuePluginEvent(_renderThreadPointer.Value, 0);
                        AssetIntegratorPatch.ProcessQueueMethod.Invoke(assetIntegrator, new object[] {2, false});

                        if (ShouldRefreshAllConnectors)
                        {
                            ShouldRefreshAllConnectors = false;
                            RefreshAllConnectors();
                        }

                        //prevent people from crashing themselves by setting it to a really low number
                        if (AutoLocalRefreshTick > 300)
                        {
                            _autoLocalRefreshTicks++;
                            if (_autoLocalRefreshTicks > AutoLocalRefreshTick)
                            {
                                _autoLocalRefreshTicks = 0;
                                RefreshAllLocalConnectors();
                            }
                        }

                        var focusedWorld = ___engine.WorldManager.FocusedWorld;
                        if (focusedWorld != null)
                        {
                            var num = ___engine.InputInterface.VR_Active;
                            var headOutput1 = num ? ___vrOutputRoot : ___screenOutputRoot;
                            var headOutput2 = num ? ___screenOutputRoot : ___vrOutputRoot;
                            if (headOutput2 != null &&
                                headOutput2.gameObject.activeSelf)
                                headOutput2.gameObject.SetActive(false);
                            if (!headOutput1.gameObject.activeSelf)
                                headOutput1.gameObject.SetActive(true);
                            headOutput1.UpdatePositioning(focusedWorld);
                            Vector3 vector3;
                            Quaternion quaternion;
                            if (focusedWorld.OverrideEarsPosition)
                            {
                                vector3 = focusedWorld.LocalUserEarsPosition.ToUnity();
                                quaternion = focusedWorld.LocalUserEarsRotation.ToUnity();
                            }
                            else
                            {
                                var cameraRoot = headOutput1.CameraRoot;
                                vector3 = cameraRoot.position;
                                quaternion = cameraRoot.rotation;
                            }

                            var transform = ___audioListener.transform;
                            transform.position = vector3;
                            transform.rotation = quaternion;
                            ___engine.WorldManager.GetWorlds(____worlds);
                            var transform1 = headOutput1.transform;
                            foreach (var world in ____worlds)
                            {
                                if (world.Focus != World.WorldFocus.Overlay &&
                                    world.Focus != World.WorldFocus.PrivateOverlay) continue;
                                var transform2 = ((WorldConnector) world.Connector).WorldRoot.transform;
                                var userGlobalPosition = world.LocalUserGlobalPosition;
                                var userGlobalRotation = world.LocalUserGlobalRotation;
                                transform2.transform.position = transform1.position - userGlobalPosition.ToUnity();
                                transform2.transform.rotation = transform1.rotation * userGlobalRotation.ToUnity();
                                transform2.transform.localScale = transform1.localScale;
                            }

                            ____worlds.Clear();
                        }

                        if (focusedWorld != ____lastFocusedWorld)
                        {
                            FrooxEngineRunner.UpdateDynamicGI(true);
                            ____lastFocusedWorld = focusedWorld;
                            __instance.StartCoroutine(UpdateDynamicGIDelayed(__instance));
                        }

                        var num1 = ___engine.InputInterface.VR_Active ? 1 : 0;
                        var lastVractive = ____lastVRactive;
                        var num2 = lastVractive.GetValueOrDefault() ? 1 : 0;
                        if (!(num1 == num2 & lastVractive.HasValue))
                        {
                            ____lastVRactive = ___engine.InputInterface.VR_Active;
                            if (____lastVRactive.Value)
                            {
                                QualitySettings.lodBias = 3.8f;
                                QualitySettings.vSyncCount = 0;
                                QualitySettings.maxQueuedFrames = 0;
                            }
                            else
                            {
                                QualitySettings.lodBias = 2f;
                                QualitySettings.vSyncCount = 1;
                                QualitySettings.maxQueuedFrames = 2;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    UniLog.Error(ex.ToString());
                    Debug.LogError(ex.ToString());
                    ___engine = null;
                    QuitApplication(__instance);
                }

                ____externalStopwatch.Restart();
                ___stopwatch.Stop();
                if (___updateDynamicGI && (___updateDynamicGIimmediate || Time.time - ___lastDynamicGIupdate > 1.0))
                {
                    __instance.GlobalProbe.RenderProbe();
                    DynamicGI.UpdateEnvironment();
                    ___updateDynamicGI = false;
                    ___updateDynamicGIimmediate = false;
                    ___lastDynamicGIupdate = Time.time;
                }

                while (___actions.TryDequeue(out var val1))
                    val1();
                //this would be a memory leak...
                //but it doesn't seem to be used anywhere...
                /*
                while (__instance.messages.TryDequeue(out var val2))
                {
                    if (val2.error)
                        __instance.UnityError(val2.msg);
                    else
                        __instance.UnityLog(val2.msg);
                }
                */
            }
            return false;
        }

        [HarmonyPatch("UpdateDynamicGIDelayed")]
        [HarmonyReversePatch]
        private static IEnumerator UpdateDynamicGIDelayed(FrooxEngineRunner instance) =>
            throw new NotImplementedException();

        [HarmonyPatch("QuitApplication")]
        [HarmonyReversePatch]
        private static void QuitApplication(FrooxEngineRunner instance) =>
            throw new NotImplementedException();
    }

    [HarmonyPatch(typeof(SystemInfoConnector))]
    public static class SystemInfoConnectorPatch
    {
        public static PropertyInfo ExternalUpdateTime =
            typeof(SystemInfoConnector).GetProperty("ExternalUpdateTime", AccessTools.all);
        public static PropertyInfo RenderTime =
            typeof(SystemInfoConnector).GetProperty("RenderTime", AccessTools.all);
        public static PropertyInfo FPS =
            typeof(SystemInfoConnector).GetProperty("FPS", AccessTools.all);
        public static PropertyInfo ImmediateFPS =
            typeof(SystemInfoConnector).GetProperty("ImmediateFPS", AccessTools.all);
        [HarmonyPatch("ExternalUpdateTime", MethodType.Setter)]
        [HarmonyReversePatch]
        public static void set_ExternalUpdateTime(SystemInfoConnector instance, float value) =>
            throw new NotImplementedException();

        [HarmonyPatch("RenderTime", MethodType.Setter)]
        [HarmonyReversePatch]
        public static void set_RenderTime(SystemInfoConnector instance, float value) =>
            throw new NotImplementedException();

        [HarmonyPatch("FPS", MethodType.Setter)]
        [HarmonyReversePatch]
        public static void set_FPS(SystemInfoConnector instance, float value) =>
            throw new NotImplementedException();

        [HarmonyPatch("ImmediateFPS", MethodType.Setter)]
        [HarmonyReversePatch]
        public static void set_ImmediateFPS(SystemInfoConnector instance, float value) =>
            throw new NotImplementedException();
    }
}