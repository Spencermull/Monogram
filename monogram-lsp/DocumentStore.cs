using OmniSharp.Extensions.LanguageServer.Protocol;

namespace monogram_lsp;

public class DocumentStore
{
    private readonly Dictionary<string, string> _docs = new();

    public void Update(DocumentUri uri, string text) => _docs[uri.ToString()] = text;
    public void Remove(DocumentUri uri)              => _docs.Remove(uri.ToString());
    public string? Get(DocumentUri uri)              => _docs.GetValueOrDefault(uri.ToString());
}
