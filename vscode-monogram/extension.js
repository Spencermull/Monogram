const { LanguageClient, TransportKind } = require('vscode-languageclient/node');
const vscode = require('vscode');
const path   = require('path');
const fs     = require('fs');

let client;
let runTerminal;

function activate(context) {
    const serverDll   = path.join(context.extensionPath, 'server', 'monogram-lsp.dll');
    const compilerDll = path.join(context.extensionPath, 'compiler', 'mngc.dll');

    if (!fs.existsSync(serverDll)) {
        vscode.window.showWarningMessage(
            'Monogram: language server not found. Run vscode-monogram/build-server.ps1 to enable diagnostics.',
            'Dismiss'
        );
    } else {
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
        client = new LanguageClient('monogram', 'Monogram Language Server', serverOptions, clientOptions);
        client.start();
        context.subscriptions.push({ dispose: () => client?.stop() });
    }

    const runCmd = vscode.commands.registerCommand('monogram.runFile', async () => {
        const editor = vscode.window.activeTextEditor;
        if (!editor || editor.document.languageId !== 'monogram') {
            vscode.window.showErrorMessage('Monogram: open a .mngrm file first.');
            return;
        }

        await editor.document.save();
        const filePath = editor.document.uri.fsPath;

        if (!fs.existsSync(compilerDll)) {
            vscode.window.showErrorMessage(
                'Monogram: compiler not found. Run vscode-monogram/build-server.ps1 first.'
            );
            return;
        }

        if (!runTerminal || runTerminal.exitStatus !== undefined) {
            runTerminal = vscode.window.createTerminal('Monogram');
        }
        runTerminal.show(true);
        runTerminal.sendText(`dotnet "${compilerDll}" "${filePath}"`);
    });

    context.subscriptions.push(runCmd);
}

function deactivate() {
    return client?.stop();
}

module.exports = { activate, deactivate };
