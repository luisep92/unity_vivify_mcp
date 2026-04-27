using MCPForUnity.Editor.Constants;
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
                Debug.Log($"[MCP] Bridge start requested (mode: {(EditorConfigurationCache.Instance.UseHttpTransport ? "HTTP" : "stdio")}).");
            }
        }

        [MenuItem("Window/MCP For Unity/Bridge Status", priority = 2)]
        public static void BridgeStatus()
        {
            var bridge = MCPServiceLocator.Bridge;
            Debug.Log($"[MCP] Bridge running: {bridge.IsRunning} (mode: {(EditorConfigurationCache.Instance.UseHttpTransport ? "HTTP" : "stdio")}, port: {bridge.CurrentPort})");
        }

        [MenuItem("Window/MCP For Unity/Use Stdio Transport", priority = 20)]
        public static void UseStdioTransport()
        {
            EditorConfigurationCache.Instance.SetUseHttpTransport(false);
            Debug.Log("[MCP] Transport set to stdio. Restart the bridge if it was running.");
        }

        [MenuItem("Window/MCP For Unity/Use HTTP Transport", priority = 21)]
        public static void UseHttpTransport()
        {
            EditorConfigurationCache.Instance.SetUseHttpTransport(true);
            Debug.Log("[MCP] Transport set to HTTP. Restart the bridge if it was running.");
        }
    }
}
