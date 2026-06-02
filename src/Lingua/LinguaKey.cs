namespace Irihi.Lingua;

/// <summary>
/// Strongly-typed key that pairs a resource key string with its owning manager.
/// Used by <see cref="TranslateExtension"/> and generated <c>Keys</c> classes.
/// </summary>
public sealed class LinguaKey
{
    public string Key { get; }
    public ILinguaManager Manager { get; }

    public LinguaKey(string key, ILinguaManager manager)
    {
        Key = key;
        Manager = manager;
    }

    public override string ToString() => Key;
}
