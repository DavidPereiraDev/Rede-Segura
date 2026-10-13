$ErrorActionPreference = "Stop"

$root = $PSScriptRoot
$compiler = Join-Path $env:WINDIR "Microsoft.NET\Framework64\v4.0.30319\csc.exe"
$temporaryDirectory = Join-Path ([System.IO.Path]::GetTempPath()) ("RedeSeguraUiTests-" + [guid]::NewGuid().ToString("N"))
$testRunnerPath = Join-Path $temporaryDirectory "rede-segura-ui-tests.exe"
$sources = @(
  (Join-Path $root "rede-segura-app.cs")
  (Join-Path $root "app-shell-controls.cs")
  (Join-Path $root "analysis-result-section.cs")
  (Join-Path $root "security-content-controls.cs")
  (Join-Path $root "url-structure-analysis.cs")
  (Join-Path $root "link-checker-control.cs")
  (Join-Path $root "settings-about-controls.cs")
  (Join-Path $root "scam-analysis.cs")
  (Join-Path $root "tests\rede-segura-ui-tests.cs")
)

if (-not (Test-Path -LiteralPath $compiler -PathType Leaf)) {
  throw "Não foi encontrado o compilador C# necessário para executar os testes."
}

New-Item -ItemType Directory -Path $temporaryDirectory | Out-Null
try {
  $arguments = @(
    "/nologo"
    "/target:exe"
    "/main:RedeSeguraUiTests"
    "/reference:System.Windows.Forms.dll"
    "/reference:System.Drawing.dll"
    "/out:$testRunnerPath"
  ) + $sources
  & $compiler $arguments
  if ($LASTEXITCODE -ne 0 -or -not (Test-Path -LiteralPath $testRunnerPath -PathType Leaf)) {
    throw "Não foi possível compilar os testes de interface."
  }

  & $testRunnerPath
  if ($LASTEXITCODE -ne 0) {
    throw "Os testes de interface falharam."
  }
} finally {
  if (Test-Path -LiteralPath $temporaryDirectory -PathType Container) {
    Remove-Item -LiteralPath $temporaryDirectory -Recurse -Force
  }
}
