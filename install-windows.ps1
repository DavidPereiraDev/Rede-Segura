param(
  [string]$InstallDirectory = (Join-Path $env:LOCALAPPDATA "Programs\Rede Segura"),
  [switch]$SkipShortcuts,
  [switch]$SkipSuccessDialog
)

$ErrorActionPreference = "Stop"

$files = @(
  "RedeSegura.exe"
)

try {
  New-Item -ItemType Directory -Path $installDirectory -Force | Out-Null
  foreach ($file in $files) {
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot $file) -Destination (Join-Path $installDirectory $file) -Force
  }

  $iconPath = Join-Path $installDirectory "RedeSegura.ico"
  $iconProcess = Start-Process `
    -FilePath (Join-Path $installDirectory "RedeSegura.exe") `
    -ArgumentList @("--export-icon", "`"$iconPath`"") `
    -Wait `
    -PassThru
  if ($iconProcess.ExitCode -ne 0 -or -not (Test-Path -LiteralPath $iconPath -PathType Leaf)) {
    throw "Não foi possível criar o ícone da aplicação."
  }

  if (-not $SkipShortcuts) {
    $shell = New-Object -ComObject WScript.Shell
    $shortcutDirectories = @(
      [Environment]::GetFolderPath("Programs"),
      [Environment]::GetFolderPath("Desktop")
    )
    foreach ($shortcutDirectory in $shortcutDirectories) {
      $shortcut = $shell.CreateShortcut((Join-Path $shortcutDirectory "Rede Segura.lnk"))
      $shortcut.TargetPath = Join-Path $installDirectory "RedeSegura.exe"
      $shortcut.Arguments = ""
      $shortcut.WorkingDirectory = $installDirectory
      $shortcut.Description = "Abrir a aplicação Rede Segura"
      $shortcut.IconLocation = "$iconPath,0"
      $shortcut.Save()
    }
  }

  if (-not $SkipSuccessDialog) {
    Add-Type -AssemblyName PresentationFramework
    [System.Windows.MessageBox]::Show(
      "A aplicação Rede Segura foi instalada. Abra o atalho com a logo no Ambiente de Trabalho ou no menu Iniciar.",
      "Rede Segura instalada",
      [System.Windows.MessageBoxButton]::OK,
      [System.Windows.MessageBoxImage]::Information
    ) | Out-Null
  }
} catch {
  Add-Type -AssemblyName PresentationFramework
  [System.Windows.MessageBox]::Show(
    "Não foi possível instalar a Rede Segura.`n`n$($_.Exception.Message)",
    "Erro de instalação",
    [System.Windows.MessageBoxButton]::OK,
    [System.Windows.MessageBoxImage]::Error
  ) | Out-Null
  exit 1
}
