using System;
using MCPForUnity.Editor.Services.Transport;

namespace MCPForUnity.Editor.Services
{
    public static class MCPServiceLocator
    {
        private static IBridgeControlService _bridgeService;
        private static IPathResolverService _pathService;
        private static IPlatformService _platformService;
        private static IToolDiscoveryService _toolDiscoveryService;
        private static TransportManager _transportManager;

        public static IBridgeControlService Bridge => _bridgeService ?? (_bridgeService = new BridgeControlService());
        public static IPathResolverService Paths => _pathService ?? (_pathService = new PathResolverService());
        public static IPlatformService Platform => _platformService ?? (_platformService = new PlatformService());
        public static IToolDiscoveryService ToolDiscovery => _toolDiscoveryService ?? (_toolDiscoveryService = new ToolDiscoveryService());
        public static TransportManager TransportManager => _transportManager ?? (_transportManager = new TransportManager());

        public static void Register<T>(T implementation) where T : class
        {
            if (implementation is IBridgeControlService b)
                _bridgeService = b;
            else if (implementation is IPathResolverService p)
                _pathService = p;
            else if (implementation is IPlatformService ps)
                _platformService = ps;
            else if (implementation is IToolDiscoveryService td)
                _toolDiscoveryService = td;
            else if (implementation is TransportManager tm)
                _transportManager = tm;
        }

        public static void Reset()
        {
            (_bridgeService as IDisposable)?.Dispose();
            (_pathService as IDisposable)?.Dispose();
            (_platformService as IDisposable)?.Dispose();
            (_toolDiscoveryService as IDisposable)?.Dispose();
            (_transportManager as IDisposable)?.Dispose();

            _bridgeService = null;
            _pathService = null;
            _platformService = null;
            _toolDiscoveryService = null;
            _transportManager = null;
        }
    }
}
