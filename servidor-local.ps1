$ErrorActionPreference = "Stop"
$root = [System.IO.Path]::GetFullPath($PSScriptRoot).TrimEnd([System.IO.Path]::DirectorySeparatorChar) + [System.IO.Path]::DirectorySeparatorChar
$url = "http://localhost:8765/index.html"
$edgePath = @(
  (Join-Path ([Environment]::GetFolderPath("ProgramFilesX86")) "Microsoft\Edge\Application\msedge.exe"),
  (Join-Path ([Environment]::GetFolderPath("ProgramFiles")) "Microsoft\Edge\Application\msedge.exe"),
  (Join-Path $env:LOCALAPPDATA "Microsoft\Edge\Application\msedge.exe")
) | Where-Object { Test-Path -LiteralPath $_ -PathType Leaf } | Select-Object -First 1

if (-not $edgePath) {
  Write-Host "O Microsoft Edge não foi encontrado. Instale o Microsoft Edge para abrir a aplicação." -ForegroundColor Red
  exit 1
}

$listener = [System.Net.HttpListener]::new()
$listener.Prefixes.Add("http://localhost:8765/")

try {
  $listener.Start()
} catch {
  $client = [System.Net.Sockets.TcpClient]::new()
  try {
    $client.Connect("localhost", 8765)
    $existingApp = Invoke-WebRequest -Uri $url -UseBasicParsing -TimeoutSec 3
    if ($existingApp.StatusCode -ne 200 -or $existingApp.Content -notmatch '<h2 id="install-title">Instale a Rede Segura</h2>') {
      throw "A porta 8765 já está a ser utilizada por outra aplicação."
    }
    Start-Process -FilePath $edgePath -ArgumentList @("--app=$url", "--no-first-run", "--no-default-browser-check")
    exit 0
  } catch {
    Write-Host "Nao foi possivel abrir a aplicacao Rede Segura: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
  } finally {
    $client.Dispose()
  }
}

$mimeTypes = @{
  ".html" = "text/html; charset=utf-8"
  ".css" = "text/css; charset=utf-8"
  ".svg" = "image/svg+xml"
}

Write-Host "Rede Segura disponivel em $url"
Start-Process -FilePath $edgePath -ArgumentList @("--app=$url", "--no-first-run", "--no-default-browser-check")

try {
  while ($listener.IsListening) {
    $context = $listener.GetContext()
    $response = $context.Response

    try {
      if ($context.Request.HttpMethod -ne "GET") {
        $response.StatusCode = 405
        $response.Close()
        continue
      }

      $relativePath = [System.Uri]::UnescapeDataString($context.Request.Url.AbsolutePath).TrimStart("/")
      if ([string]::IsNullOrWhiteSpace($relativePath)) {
        $relativePath = "index.html"
      }

      $relativePath = $relativePath.Replace("/", [System.IO.Path]::DirectorySeparatorChar)
      $filePath = [System.IO.Path]::GetFullPath([System.IO.Path]::Combine($root, $relativePath))
      if (-not $filePath.StartsWith($root, [System.StringComparison]::OrdinalIgnoreCase)) {
        $response.StatusCode = 403
        $response.Close()
        continue
      }

      if (-not [System.IO.File]::Exists($filePath)) {
        $response.StatusCode = 404
        $response.Close()
        continue
      }

      $extension = [System.IO.Path]::GetExtension($filePath).ToLowerInvariant()
      $response.ContentType = if ($mimeTypes.ContainsKey($extension)) { $mimeTypes[$extension] } else { "application/octet-stream" }
      $response.Headers["Cache-Control"] = "no-cache"
      $bytes = [System.IO.File]::ReadAllBytes($filePath)
      $response.ContentLength64 = $bytes.Length
      $response.OutputStream.Write($bytes, 0, $bytes.Length)
      $response.Close()
    } catch {
      Write-Host "Erro ao responder ao pedido: $($_.Exception.Message)" -ForegroundColor Red
      $response.StatusCode = 500
      $response.Close()
    }
  }
} finally {
  $listener.Stop()
  $listener.Close()
}
