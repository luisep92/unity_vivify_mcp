using MCPForUnity.Editor.Services;
using UnityEditor;
using UnityEngine;

namespace MCPForUnity.Editor.MenuItems
{
    public static class MCPForUnityMenu
    {
        [MenuItem("Window/MCP For Unity/Toggle Bridge", priority = 1)]
        public static void ToggleBridge()
        {
            var bridge = MCPServiceLocator.Bridge;
            if (bridge.IsRunning)
            {
                _ = bridge.StopAsync();
                Debug.Log("[MCP] Bridge stop requested.");
            }
            else
            {
                _ = bridge.StartAsync();
                Debug.Log("[MCP] Bridge start requested.");
            }
        }

        [MenuItem("Window/MCP For Unity/Bridge Status", priority = 2)]
        public static void BridgeStatus()
        {
            var bridge = MCPServiceLocator.Bridge;
            Debug.Log($"[MCP] Bridge running: {bridge.IsRunning}");
        }
    }
}
