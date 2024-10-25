// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Management.Automation;
using System.Management.Automation.Subsystem;
using System.Management.Automation.Subsystem.Feedback;
using System.Management.Automation.Subsystem.Prediction;
using Microsoft.Extensions.ObjectPool;

namespace Microsoft.WinGet.CommandNotFound
{
    public sealed class WinGetCommandNotFoundFeedbackPredictor : IFeedbackProvider, ICommandPredictor, IModuleAssemblyInitializer, IModuleAssemblyCleanup
    {
        private readonly Guid _guid = new Guid("09cd038b-a75f-4d91-8f71-f29e1ab480dc");

        private readonly ObjectPool<System.Management.Automation.PowerShell> _pool;

        private const int _maxSuggestions = 20;

        private List<string> _candidates = new List<string>();

        private bool _warmedUp;

        private WinGetCommandNotFoundFeedbackPredictor()
        {
            var provider = new DefaultObjectPoolProvider();
            _pool = provider.Create(new PooledPowerShellObjectPolicy());
            _pool.Return(_pool.Get());
        }

        public Guid Id => _guid;

        public string Name => "Windows Package Manager - WinGet";

        public string Description => "Finds missing commands that can be installed via WinGet.";

        public Dictionary<string, string>? FunctionsToDefine => null;

        private async void WarmUp()
        {
            var ps = _pool.Get();
            try
            {
                await ps.AddCommand("Find-WinGetPackage")
                    .AddParameter("Count", 1)
                    .InvokeAsync();
            }
            catch (Exception /*ex*/) {}
            finally
            {
                _pool.Return(ps);
                _warmedUp = true;
            }
        }

        public void OnImport()
        {
            if (!Platform.IsWindows || !IsWinGetInstalled())
            {
                return;
            }

            WarmUp();

            SubsystemManager.RegisterSubsystem(SubsystemKind.FeedbackProvider, this);
            SubsystemManager.RegisterSubsystem(SubsystemKind.CommandPredictor, this);
        }

        public void OnRemove(PSModuleInfo psModuleInfo)
        {
            if (!Platform.IsWindows || !IsWinGetInstalled())
            {
                return;
            }

            SubsystemManager.UnregisterSubsystem<IFeedbackProvider>(Id);
            SubsystemManager.UnregisterSubsystem<ICommandPredictor>(Id);
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

        /// <summary>
        /// Gets feedback based on the given commandline and error record.
        /// </summary>
        public FeedbackItem? GetFeedback(FeedbackContext context, CancellationToken token)
        {
            var target = (string)context.LastError!.TargetObject;
            if (target is null)
            {
                return null;
            }

            try
            {
                bool tooManySuggestions = false;
                string packageMatchFilterField = "command";
                var pkgList = FindPackages(target, ref tooManySuggestions, ref packageMatchFilterField);
                if (pkgList.Count == 0)
                {
                    return null;
                }

                // Build list of suggestions
                _candidates.Clear();
                foreach (var pkg in pkgList)
                {
                    _candidates.Add(string.Format(CultureInfo.InvariantCulture, "winget install --id {0}", pkg.Members["Id"].Value.ToString()));
                }

                // Build footer message
                var footerMessage = tooManySuggestions ?
                    string.Format(CultureInfo.InvariantCulture, "Additional results can be found using \"winget search --{0} {1}\"", packageMatchFilterField, target) :
                    null;

                return new FeedbackItem(
                    "Try installing this package using WinGet:",
                    _candidates,
                    footerMessage,
                    FeedbackDisplayLayout.Portrait);
            }
            catch (Exception /*ex*/)
            {
                return new FeedbackItem($"Failed to execute WinGet Command Not Found.{Environment.NewLine}This is a known issue if PowerShell 7 is installed from the Store or MSIX (see https://github.com/microsoft/winget-command-not-found/issues/3). If that isn't your case, please report an issue.", new List<string>(), FeedbackDisplayLayout.Portrait);
            }
        }

        private Collection<PSObject> FindPackages(string query, ref bool tooManySuggestions, ref string packageMatchFilterField)
        {
            if (!_warmedUp)
            {
                // Given that the warm-up was not done, it's no good to carry on because we
                // will likely get a newly created PowerShell object
                // and pay the same overhead of the warmup method.
                return new Collection<PSObject>();
            }

            var ps = _pool.Get();
            try
            {
                var common = new Hashtable()
                {
                    ["Source"] = "winget",
                };

                // 1) Search by command
                var pkgList = ps.AddCommand("Find-WinGetPackage")
                    .AddParameter("Command", query)
                    .AddParameter("MatchOption", "StartsWithCaseInsensitive")
                    .AddParameters(common)
                    .Invoke();
                if (pkgList.Count > 0)
                {
                    tooManySuggestions = pkgList.Count > _maxSuggestions;
                    packageMatchFilterField = "command";
                    return pkgList;
                }

                // 2) No matches found,
                //    search by name
                ps.Commands.Clear();
                pkgList = ps.AddCommand("Find-WinGetPackage")
                    .AddParameter("Name", query)
                    .AddParameter("MatchOption", "ContainsCaseInsensitive")
                    .AddParameters(common)
                    .Invoke();
                if (pkgList.Count > 0)
                {
                    tooManySuggestions = pkgList.Count > _maxSuggestions;
                    packageMatchFilterField = "name";
                    return pkgList;
                }

                // 3) No matches found,
                //    search by moniker
                ps.Commands.Clear();
                pkgList = ps.AddCommand("Find-WinGetPackage")
                    .AddParameter("Moniker", query)
                    .AddParameter("MatchOption", "ContainsCaseInsensitive")
                    .AddParameters(common)
                    .Invoke();
                tooManySuggestions = pkgList.Count > _maxSuggestions;
                packageMatchFilterField = "moniker";
                return pkgList;
            }
            finally
            {
                _pool.Return(ps);
            }
        }

        public bool CanAcceptFeedback(PredictionClient client, PredictorFeedbackKind feedback)
        {
            return feedback switch
            {
                PredictorFeedbackKind.CommandLineAccepted => true,
                _ => false,
            };
        }

        public SuggestionPackage GetSuggestion(PredictionClient client, PredictionContext context, CancellationToken cancellationToken)
        {
            if (_candidates.Count() > 0)
            {
                string input = context.InputAst.Extent.Text;
                List<PredictiveSuggestion>? result = null;

                foreach (string c in _candidates)
                {
                    if (c.StartsWith(input, StringComparison.OrdinalIgnoreCase))
                    {
                        result ??= new List<PredictiveSuggestion>(_candidates.Count);
                        result.Add(new PredictiveSuggestion(c));
                    }
                }

                if (result is not null)
                {
                    return new SuggestionPackage(result);
                }
            }

            return default;
        }

        public void OnCommandLineAccepted(PredictionClient client, IReadOnlyList<string> history)
        {
            // Reset the candidate state.
            _candidates.Clear();
        }
    }
}
