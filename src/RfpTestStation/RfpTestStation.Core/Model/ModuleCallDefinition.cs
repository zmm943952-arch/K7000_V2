namespace RfpTestStation.Core.Model
{
    public sealed class ModuleCallDefinition
    {
        public string? AdapterName { get; set; }

        public string? AssemblyName { get; set; }

        public string? TypeName { get; set; }

        public string? MethodName { get; set; }

        public string? ViPath { get; set; }

        public string? RawText { get; set; }
    }
}
