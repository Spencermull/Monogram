# Publishes monogram-lsp into vscode-monogram/server/ so the extension can find it.
# Run this once after cloning, and again whenever the language server changes.

$root      = Split-Path -Parent $PSScriptRoot
$lspProj   = Join-Path $root "monogram-lsp" "monogram-lsp.csproj"
$outputDir = Join-Path $PSScriptRoot "server"

Write-Host "Building monogram-lsp..."
dotnet publish $lspProj -c Release -o $outputDir --self-contained false /nologo

if ($LASTEXITCODE -eq 0) {
    Write-Host "Server published to $outputDir"
    Write-Host "Reload VS Code to activate the language server."
} else {
    Write-Error "Build failed."
}
