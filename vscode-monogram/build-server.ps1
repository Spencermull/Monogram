# Publishes monogram-lsp and mngc into vscode-monogram/ so the extension can find them.
# Run this once after cloning, and again whenever either project changes.

$root        = Split-Path -Parent $PSScriptRoot
$lspProj     = Join-Path $root "monogram-lsp" "monogram-lsp.csproj"
$compilerProj = Join-Path $root "mngc" "mngc.csproj"
$serverDir   = Join-Path $PSScriptRoot "server"
$compilerDir = Join-Path $PSScriptRoot "compiler"

Write-Host "Building monogram-lsp..."
dotnet publish $lspProj -c Release -o $serverDir --self-contained false /nologo
if ($LASTEXITCODE -ne 0) { Write-Error "LSP build failed."; exit 1 }

Write-Host "Building mngc..."
dotnet publish $compilerProj -c Release -o $compilerDir --self-contained false /nologo
if ($LASTEXITCODE -ne 0) { Write-Error "Compiler build failed."; exit 1 }

Write-Host "Done. Reload VS Code to activate."
