$ErrorActionPreference = "Stop"

$root = $PSScriptRoot
$compiler = Join-Path $env:WINDIR "Microsoft.NET\Framework64\v4.0.30319\csc.exe"
$testRunnerSource = Join-Path $root "tests\url-structure-analyzer-tests.cs"
$analyzerSource = Join-Path $root "url-structure-analysis.cs"
$temporaryDirectory = Join-Path ([System.IO.Path]::GetTempPath()) ("RedeSeguraUrlTests-" + [guid]::NewGuid().ToString("N"))
$testRunnerPath = Join-Path $temporaryDirectory "url-structure-analyzer-tests.exe"

if (-not (Test-Path -LiteralPath $compiler -PathType Leaf)) {
  throw "Não foi encontrado o compilador C# necessário para executar os testes."
}

New-Item -ItemType Directory -Path $temporaryDirectory | Out-Null
try {
  & $compiler /nologo /target:exe "/out:$testRunnerPath" $analyzerSource $testRunnerSource
  if ($LASTEXITCODE -ne 0 -or -not (Test-Path -LiteralPath $testRunnerPath -PathType Leaf)) {
    throw "Não foi possível compilar o conjunto de testes de análise de URLs."
  }

  & $testRunnerPath
  if ($LASTEXITCODE -ne 0) {
    throw "Os testes de análise de URLs falharam."
  }
} finally {
  if (Test-Path -LiteralPath $temporaryDirectory -PathType Container) {
    Remove-Item -LiteralPath $temporaryDirectory -Recurse -Force
  }
}
