$ErrorActionPreference = "Stop"

$root = $PSScriptRoot
$compiler = Join-Path $env:WINDIR "Microsoft.NET\Framework64\v4.0.30319\csc.exe"
$outputPath = Join-Path $root "RedeSegura-Setup.exe"
$appOutputPath = Join-Path $root "RedeSegura.exe"
$appSourcePath = Join-Path $root "rede-segura-app.cs"
$files = @(
  "RedeSegura.exe",
  "install-windows.ps1"
)

if (-not (Test-Path -LiteralPath $compiler)) {
  throw "Não foi encontrado o compilador C# necessário para criar o instalador."
}

$appArguments = @(
  "/nologo"
  "/target:winexe"
  "/reference:System.Windows.Forms.dll"
  "/reference:System.Drawing.dll"
  "/out:$appOutputPath"
  $appSourcePath
  (Join-Path $root "scam-analysis.cs")
)
& $compiler $appArguments
if ($LASTEXITCODE -ne 0 -or -not (Test-Path -LiteralPath $appOutputPath -PathType Leaf)) {
  throw "Não foi possível criar a aplicação Windows RedeSegura.exe."
}

$arguments = @(
  "/nologo"
  "/target:winexe"
  "/reference:System.Windows.Forms.dll"
  "/out:$outputPath"
)

foreach ($file in $files) {
  $sourcePath = Join-Path $root $file
  if (-not (Test-Path -LiteralPath $sourcePath -PathType Leaf)) {
    throw "Ficheiro necessário para o instalador não encontrado: $sourcePath"
  }
  $arguments += "-resource:$sourcePath,RedeSegura.$file"
}

$arguments += Join-Path $root "windows-installer.cs"
& $compiler $arguments
if ($LASTEXITCODE -ne 0 -or -not (Test-Path -LiteralPath $outputPath -PathType Leaf)) {
  throw "Não foi possível criar o instalador RedeSegura-Setup.exe."
}

Write-Output "Instalador criado: $outputPath"
