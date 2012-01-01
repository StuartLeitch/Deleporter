using System.Linq;

namespace DeleporterCore.SelfHosting.Servers
{
    public interface IServer {
        bool Start();

        void Stop();
    }
}