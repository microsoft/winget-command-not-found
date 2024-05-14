// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Management.Automation;
using System.Management.Automation.Runspaces;
using Microsoft.Extensions.ObjectPool;

namespace Microsoft.WinGet.CommandNotFound
{
    public sealed class PooledPowerShellObjectPolicy : IPooledObjectPolicy<System.Management.Automation.PowerShell>
    {
        private static readonly string[] WingetClientModuleName = new[] { "Microsoft.WinGet.Client" };

        private static readonly InitialSessionState _initialSessionState;

        static PooledPowerShellObjectPolicy()
        {
            _initialSessionState = InitialSessionState.CreateDefault2();
            _initialSessionState.ImportPSModule(WingetClientModuleName);
        }

        public System.Management.Automation.PowerShell Create()
        {
            return System.Management.Automation.PowerShell.Create(_initialSessionState);
        }

        public bool Return(System.Management.Automation.PowerShell obj)
        {
            if (obj != null)
            {
                obj.Commands.Clear();
                obj.Streams.ClearStreams();
                return true;
            }

            return false;
        }
    }
}
