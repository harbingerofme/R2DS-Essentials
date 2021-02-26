using System.Reflection;

namespace R2DSEssentials.Util
{
    internal static class Reflection
    {
        internal const BindingFlags AllFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static |
                                              BindingFlags.Instance | BindingFlags.DeclaredOnly;
    }
}