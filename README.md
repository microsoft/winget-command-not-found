# Microsoft.WinGet.CommandNotFound

The `Microsoft.WinGet.CommandNotFound` module is a feedback provider plugin for PowerShell. This feedback provider leverages the Windows Package Manager to provide suggestions for packages to install when a native command cannot be found.

The [command-line predictor][command-line-predictor] feature in PowerShell enables this module to display these WinGet packages as predictive suggestions.

![Demo](./images/demo.png)

## Requirements

The `Microsoft.WinGet.CommandNotFound` PowerShell Module is built on the `IFeedbackProvider` interface, which is available with PowerShell `7.4.0-preview.2` or above. To display prediction suggestions, you need [PSReadLine 2.2.6][psreadline-226] or above.

- PowerShell `7.4.0-preview.2` or above
- PSReadLine `2.2.6` or above

The following experimental features must be enabled:

- `PSFeedbackProvider`
- `PSCommandNotFoundSuggestion`

They can be enabled by running the following commands:
```PowerShell
Enable-ExperimentalFeature PSFeedbackProvider
Enable-ExperimentalFeature PSCommandNotFoundSuggestion
```

## Documentation

PowerShell feedback providers and predictors are written in C# and registered with the PowerShell [Subsystem Plugin Model][subsystem-plugin-model].
To learn more, see "How to create a feedback provider" and ["How to create a command-line predictor"][how-to-create-predictor].

## Contributing

This project welcomes contributions and suggestions.  Most contributions require you to agree to a
Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us
the rights to use your contribution. For details, visit https://cla.opensource.microsoft.com.

When you submit a pull request, a CLA bot will automatically determine whether you need to provide
a CLA and decorate the PR appropriately (e.g., status check, comment). Simply follow the instructions
provided by the bot. You will only need to do this once across all repos using our CLA.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/).
For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or
contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.

## Trademarks

This project may contain trademarks or logos for projects, products, or services. Authorized use of Microsoft 
trademarks or logos is subject to and must follow 
[Microsoft's Trademark & Brand Guidelines](https://www.microsoft.com/en-us/legal/intellectualproperty/trademarks/usage/general).
Use of Microsoft trademarks or logos in modified versions of this project must not cause confusion or imply Microsoft sponsorship.
Any use of third-party trademarks or logos are subject to those third-party's policies.

[command-line-predictor]: https://learn.microsoft.com/en-us/powershell/scripting/learn/shell/using-predictors
[psreadline-226]: https://www.powershellgallery.com/packages/PSReadLine/2.2.6
[subsystem-plugin-model]: https://docs.microsoft.com/powershell/scripting/learn/experimental-features#pssubsystempluginmodel
[how-to-create-predictor]: https://docs.microsoft.com/powershell/scripting/dev-cross-plat/create-cmdline-predictor