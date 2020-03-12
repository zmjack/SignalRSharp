using System;

namespace Appie
{
    public static class SignalRDelegate
    {
        public delegate void SRAction(SignalRClient sender);
        public delegate void SRException(SignalRClient sender, Exception exception);
    }
}
