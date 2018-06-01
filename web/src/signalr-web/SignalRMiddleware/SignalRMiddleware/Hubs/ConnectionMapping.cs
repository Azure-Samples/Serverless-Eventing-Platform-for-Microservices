using System.Collections.Generic;
using System.Linq;

namespace SignalRMiddleware.Hubs

{
    public static class Connections
    {
        public readonly static ConnectionMapping<string> _connections =
           new ConnectionMapping<string>();
    }
    public class ConnectionMapping<T>
    {
        private readonly Dictionary<T, HashSet<string>> _connections =
            new Dictionary<T, HashSet<string>>();

        public int Count => _connections.Count;
       
        public void Add(T key, string connectionId)
        {
            lock (_connections)
            {
                HashSet<string> clientConnections;
                if (!_connections.TryGetValue(key, out clientConnections))
                {
                    clientConnections = new HashSet<string>();
                    _connections.Add(key, clientConnections);
                }

                lock (clientConnections)
                {
                    clientConnections.Add(connectionId);
                }
            }
        }

        public IEnumerable<string> GetConnections(T key)
        {
            HashSet<string> clientConnections;
            if (_connections.TryGetValue(key, out clientConnections))
            {
                return clientConnections;
            }

            return Enumerable.Empty<string>();
        }

        public void Remove(T key, string connectionId)
        {
            lock (_connections)
            {
                HashSet<string> clientConnections;
                if (!_connections.TryGetValue(key, out clientConnections))
                {
                    return;
                }

                lock (clientConnections)
                {
                    clientConnections.Remove(connectionId);

                    if (clientConnections.Count == 0)
                    {
                        _connections.Remove(key);
                    }
                }
            }
        }
    }
}