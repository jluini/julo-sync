﻿namespace Julo.Network
{

    public delegate DualServer CreateServerDelegate(Mode mode);
    public delegate DualClient CreateHostedClientDelegate(Mode mode, DualServer server);
    public delegate DualClient CreateRemoteClientDelegate();

    public class DNM
    {
        public const int LocalConnectionId = 0;
        
    }

}
