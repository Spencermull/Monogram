const { LanguageClient, TransportKind } = require('vscode-languageclient/node');
const vscode = require('vscode');
const path   = require('path');
const fs     = require('fs');

let client;

function activate(context) {
    const serverDll = path.join(context.extensionPath, 'server', 'monogram-lsp.dll');

    if (!fs.existsSync(serverDll)) {
        vscode.window.showWarningMessage(
            'Monogram: language server not found. Run vscode-monogram/build-server.ps1 to enable diagnostics.',
            'Dismiss'
        );
        return;
    }

    const serverOptions = {
        run:   { command: 'dotnet', args: [serverDll], transport: TransportKind.stdio },
        debug: { command: 'dotnet', args: [serverDll], transport: TransportKind.stdio },
    };

    const clientOptions = {
        documentSelector: [{ scheme: 'file', language: 'monogram' }],
        synchronize: {
            fileEvents: vscode.workspace.createFileSystemWatcher('**/*.mngrm')
        },
    };

    client = new LanguageClient(
        'monogram',
        'Monogram Language Server',
        serverOptions,
        clientOptions
    );

    client.start();
    context.subscriptions.push({ dispose: () => client?.stop() });
}

function deactivate() {
    return client?.stop();
}

module.exports = { activate, deactivate };
