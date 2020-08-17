using System;
using R2API.Utils;

// Make R2API ignore R2DSE so that it doesnt get registered the network mod list
[assembly: ManualNetworkRegistration]

namespace R2DSEssentials
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    internal class ModuleAttribute : Attribute
    {
        public readonly string Name;
        public readonly bool DefaultEnabled;
        public readonly string Description;
        public StartupTarget target;
        public ModuleAttribute(string name, string description, bool defaultEnabled, StartupTarget target = StartupTarget.Awake)
        {
            Name = name;
            DefaultEnabled = defaultEnabled;
            Description = description;
            this.target = target;
        }

        public enum StartupTarget
        {
            Awake,
            Start
        }
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    internal class ModuleDependency : Attribute
    {
        public readonly string Dependency;
        public readonly DependencyType Type;
        public ModuleDependency(string dependency, DependencyType type = DependencyType.Hard)
        {
            Dependency = dependency;
            Type = type;
        }

        public enum DependencyType
        {
            Hard,
            Soft
        }
    }

    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
    internal class PluginDependency : Attribute
    {
        public string GUID;
    }
}

namespace R2API.Utils
{
    [AttributeUsage(AttributeTargets.Assembly)]
    public class ManualNetworkRegistrationAttribute : Attribute { }
}