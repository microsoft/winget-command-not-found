@{
    ModuleVersion = '0.1.0'
    GUID = '7936322d-30fe-410f-b681-114fe84a65d4'
    Author = 'Microsoft Corporation'
    CompanyName = "Microsoft Corporation"
    Copyright = "Copyright (c) Microsoft Corporation."
    Description = 'Enable suggestions on how to install missing commands via winget'
    PowerShellVersion = '7.4'

    NestedModules = @('ValidateOS.psm1', 'winget-command-not-found.dll')
    FunctionsToExport = @()
    CmdletsToExport = @()
    VariablesToExport = '*'
    AliasesToExport = @()

    RequiredModules   = @(@{ModuleName = 'Microsoft.WinGet.Client'; RequiredVersion = "1.8.1133"; })

    PrivateData = @{
        PSData = @{
            Tags = @('Windows')
            ProjectUri = 'https://github.com/Microsoft/winget-command-not-found'
        }
    }
}
