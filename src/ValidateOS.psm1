function Test-WinGetExists() {
    $package = Get-Command winget -ErrorAction SilentlyContinue;
    return $package -ne $null;
}

if (!$IsWindows -or !(Test-WinGetExists)) {
    $exception = [System.PlatformNotSupportedException]::new(
        "This module only works on Windows and depends on the application 'winget.exe' to be available.")
    $err = [System.Management.Automation.ErrorRecord]::new($exception, "PlatformNotSupported", "InvalidOperation", $null)
    throw $err
}
