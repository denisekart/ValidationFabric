namespace ValidationFabric.Abstractions
{
    public interface IValidationChain<T>
    {
        string Name { get; }
        bool IsCompiled { get; }
        bool CanModify { get; }
        ValidationResult Invoke(T item);
    }
}