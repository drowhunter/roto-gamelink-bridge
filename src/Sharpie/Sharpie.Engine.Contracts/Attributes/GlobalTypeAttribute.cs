namespace Sharpie.Engine.Contracts
{
    [AttributeUsage(AttributeTargets.Class)]
    public class GlobalTypeAttribute : Attribute
    {
        public Type Type { get; set; }

        public bool IsIndexed { get; set; }
    }
}
