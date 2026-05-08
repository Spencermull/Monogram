using MediatR;
using mngc.Lexer;
using mngc.Parser;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using OmniSharp.Extensions.LanguageServer.Protocol.Server.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Workspace;

namespace monogram_lsp.Handlers;

public class TextDocumentHandler : TextDocumentSyncHandlerBase
{
    private readonly ILanguageServerFacade _facade;
    private readonly DocumentStore _store;

    public static readonly TextDocumentSelector Selector = new(
        new TextDocumentFilter { Language = "monogram", Pattern = "**/*.mngrm" }
    );

    public TextDocumentHandler(ILanguageServerFacade facade, DocumentStore store)
    {
        _facade = facade;
        _store  = store;
    }

    public override TextDocumentAttributes GetTextDocumentAttributes(DocumentUri uri) => new(uri, "monogram");

    public override Task<Unit> Handle(DidOpenTextDocumentParams request, CancellationToken ct)
    {
        _store.Update(request.TextDocument.Uri, request.TextDocument.Text);
        Diagnose(request.TextDocument.Uri, request.TextDocument.Text);
        return Unit.Task;
    }

    public override Task<Unit> Handle(DidChangeTextDocumentParams request, CancellationToken ct)
    {
        var text = request.ContentChanges.LastOrDefault()?.Text ?? "";
        _store.Update(request.TextDocument.Uri, text);
        Diagnose(request.TextDocument.Uri, text);
        return Unit.Task;
    }

    public override Task<Unit> Handle(DidSaveTextDocumentParams request, CancellationToken ct)
        => Unit.Task;

    public override Task<Unit> Handle(DidCloseTextDocumentParams request, CancellationToken ct)
    {
        _store.Remove(request.TextDocument.Uri);
        // Clear diagnostics on close
        _facade.TextDocument.PublishDiagnostics(new PublishDiagnosticsParams
        {
            Uri = request.TextDocument.Uri,
            Diagnostics = new Container<Diagnostic>()
        });
        return Unit.Task;
    }

    private void Diagnose(DocumentUri uri, string text)
    {
        var diagnostics = new List<Diagnostic>();
        try
        {
            var tokens = new Lexer().Tokenize(text);
            new Parser(tokens).Parse();
        }
        catch (ParseException ex)
        {
            // LSP positions are 0-based; ParseException is 1-based
            var line = Math.Max(0, ex.Line - 1);
            var col  = Math.Max(0, ex.Column - 1);
            diagnostics.Add(new Diagnostic
            {
                Range    = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(
                               new Position(line, col),
                               new Position(line, col + 1)),
                Message  = ex.Message,
                Severity = DiagnosticSeverity.Error,
                Source   = "mngc",
            });
        }

        _facade.TextDocument.PublishDiagnostics(new PublishDiagnosticsParams
        {
            Uri         = uri,
            Diagnostics = new Container<Diagnostic>(diagnostics),
        });
    }

    protected override TextDocumentSyncRegistrationOptions CreateRegistrationOptions(
        TextSynchronizationCapability capability,
        ClientCapabilities clientCapabilities) => new()
    {
        DocumentSelector = Selector,
        Change           = TextDocumentSyncKind.Full,
        Save             = new SaveOptions { IncludeText = false },
    };
}
