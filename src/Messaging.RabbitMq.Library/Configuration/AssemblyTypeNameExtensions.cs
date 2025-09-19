namespace Messaging.RabbitMq.Library.Configuration;

public static class AssemblyTypeNameExtensions
{
    public static (string typeName, string assemblyName) SplitFqn(this string typeFullName)
    {
        ArgumentException.ThrowIfNullOrEmpty(typeFullName);
        var parts = typeFullName.Split(',', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length is not 2 and not 5) throw new InvalidOperationException($"{nameof(AssemblyTypeNameExtensions)} - Invalid {nameof(typeFullName)} - {typeFullName}");
        var typeName = parts[0];
        var assemblyName = string.Join(',', parts.Skip(1).ToArray());
        return (typeName, assemblyName);
    }

    public static (string typeName, string assemblyName) ShortSplitFqn(this string typeFullName)
    {
        ArgumentException.ThrowIfNullOrEmpty(typeFullName);
        var parts = typeFullName.Split(',', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length is not 2 and not 5) throw new InvalidOperationException($"{nameof(AssemblyTypeNameExtensions)} - Invalid {nameof(typeFullName)} - {typeFullName}");
        var typeName = parts[0];
        var assemblyName = string.Join(',', parts.Skip(1).Take(1).ToArray());
        return (typeName, assemblyName);
    }

    public static (string typeName, string assemblyName) ShortSplitFqn(this Type type)
    {
        ArgumentNullException.ThrowIfNull(type, nameof(Type));
        var typeName = type.UnderlyingSystemType.FullName!;
        var assemblyName = type.Assembly.GetName().Name!;
        return (typeName, assemblyName);
    }
    public static string GetFqn(string typeFullName, string assemblyBase)
    {
        return $"{typeFullName},{assemblyBase}";
    }

    public static string GetTypeName<T>()
    {
        var t = typeof(T);
        return t.GetTypeName();
    }

    public static string GetTypeName(this Type t)
    {
        return t.AssemblyQualifiedName!;
    }

    public static (Type type, string fqn) GetTypeAssemblyName<T>()
    {
        var t = typeof(T);
        return (t, t.GetTypeName());
    }

    public static (Type type, string fqn) GetTypesShortAssemblyName<T>()
    {
        var t = typeof(T);
        var (typeName, assemblyShortName) = t.ShortSplitFqn();
        return (t, $"{typeName},{assemblyShortName}");
    }
}