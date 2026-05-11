namespace mngc.TypeChecker;

public enum SymbolKind { Variable, Function, TypeAlias, Parameter }

public record Symbol(string Name, MgType Type, SymbolKind Kind);

public class Scope
{
    private readonly Stack<Dictionary<string, Symbol>> _frames = new();

    public Scope() => Push();   // global frame

    public void Push() => _frames.Push(new Dictionary<string, Symbol>());

    public void Pop()
    {
        if (_frames.Count > 1) _frames.Pop();
    }

    // Returns false if a symbol with the same name already exists in the current frame.
    public bool TryDeclare(Symbol symbol)
    {
        var frame = _frames.Peek();
        if (frame.ContainsKey(symbol.Name)) return false;
        frame[symbol.Name] = symbol;
        return true;
    }

    // Walks all frames from innermost outward.
    public Symbol? Resolve(string name)
    {
        foreach (var frame in _frames)
            if (frame.TryGetValue(name, out var sym)) return sym;
        return null;
    }
}
