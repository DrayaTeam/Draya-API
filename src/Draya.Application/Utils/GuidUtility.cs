using System;

namespace Draya.Application.Utils;

public static class GuidUtility
{
    public static readonly Guid IsoOidNamespace = new Guid("6ba7b812-9dad-11d1-80b4-00c04fd430c8");

    public static Guid Create(Guid namespaceId, string name)
    {
        // Simple deterministic guid generator for the sake of example.
        using (var md5 = System.Security.Cryptography.MD5.Create())
        {
            var hash = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(namespaceId.ToString() + name));
            return new Guid(hash);
        }
    }
}
