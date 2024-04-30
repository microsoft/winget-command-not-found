// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Management.Automation;
using System.Management.Automation.Subsystem;
using System.Management.Automation.Subsystem.Feedback;
using System.Management.Automation.Subsystem.Prediction;

namespace Microsoft.WinGet.CommandNotFound
{
    public sealed class Init : IModuleAssemblyInitializer, IModuleAssemblyCleanup
    {
        internal const string Id = "09cd038b-a75f-4d91-8f71-f29e1ab480dc";

        public void OnImport()
        {
            if (!Platform.IsWindows || !IsWinGetInstalled())
            {
                return;
            }

            SubsystemManager.RegisterSubsystem(SubsystemKind.FeedbackProvider, WinGetCommandNotFoundFeedbackPredictor.Singleton);
            SubsystemManager.RegisterSubsystem(SubsystemKind.CommandPredictor, WinGetCommandNotFoundFeedbackPredictor.Singleton);
        }

        public void OnRemove(PSModuleInfo psModuleInfo)
        {
            if (!Platform.IsWindows || !IsWinGetInstalled())
            {
                return;
            }

            SubsystemManager.UnregisterSubsystem<IFeedbackProvider>(new Guid(Id));
            SubsystemManager.UnregisterSubsystem<ICommandPredictor>(new Guid(Id));
        }

        private bool IsWinGetInstalled()
        {
            // Ensure WinGet is installed
            using (var pwsh = System.Management.Automation.PowerShell.Create(RunspaceMode.CurrentRunspace))
            {
                var results = pwsh.AddCommand("Get-Command")
                    .AddParameter("Name", "winget")
                    .Invoke();

                if (results.Count is 0)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
