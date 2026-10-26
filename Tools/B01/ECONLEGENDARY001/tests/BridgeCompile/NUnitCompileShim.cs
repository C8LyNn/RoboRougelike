// .NET8 COMPILE ONLY, NOT native NUnit execution; used to catch syntax/signature issues.
using System;
namespace NUnit.Framework
{
    [AttributeUsage(AttributeTargets.Method)] public sealed class TestAttribute : Attribute {}
    public static class Assert
    {
        public static void IsFalse(bool actual){if(actual)throw new Exception("Expected false");}
        public static void AreEqual(object expected,object actual)
        {if(!Equals(expected,actual))throw new Exception("Values differ");}
    }
}
