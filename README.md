# MemberAccess

C# Source generator helping with certain tasks concerning member access, like generating public properties for private variables

Currently, there is only one generator included: ``[GeneratePropertiesForAllPrivateVariables]``

# Usage

Add a class 

    [GeneratePropertiesForAllPrivateVariables]
    public partial class Demo
    {
        int id;
        string name;
    }

This will generate the following shadow class with accessors:

    public partial class Demo
    {
        public int Id
        {
            get => id;
            set => id = value;
        }

        public string Name
        {
            get => name;
            set => name = value;
        }
    }
    
Now you can access the members by their upper-case properties:

        var demoInstance = new Demo();
        demoInstance.Id = 42;
        demoInstance.Name = "Foo";

You should make the [GeneratePropertiesForAllPrivateVariables] known to your project somewhere, so the code analyzer doesn't put a red  zig-zag line under it:

    [AttributeUsage(AttributeTargets.Class/* ToDo: | System.AttributeTargets.Struct */, AllowMultiple = false, Inherited = false)]
    public sealed class GeneratePropertiesForAllPrivateVariablesAttribute : Attribute
    {
    }
    
# Limitations

Currently only public non-static classes are supported. It's a bit hacky here and there, but should do the job in 99% of all cases. Consider it a v0.1. Look for 'ToDo', if you want to know more.

If you add some useful features or fix a bug: pull requests welcome!

# License

MIT
