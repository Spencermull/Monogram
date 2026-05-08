using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using monogram_lsp;
using monogram_lsp.Handlers;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Server;

var server = await LanguageServer.From(options => options
    .WithInput(Console.OpenStandardInput())
    .WithOutput(Console.OpenStandardOutput())
    .ConfigureLogging(x => x
        .AddLanguageProtocolLogging()
        .SetMinimumLevel(LogLevel.Warning))
    .WithServices(s => s.AddSingleton<DocumentStore>())
    .WithHandler<TextDocumentHandler>()
    .WithHandler<CompletionHandler>()
    .WithHandler<HoverHandler>()
);

await server.WaitForExit;
