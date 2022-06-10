# MemberAccess

C# Source generator helping with certain tasks concerning member access, like generating public properties for private variables.

Currently, there is only one generator ``[GeneratePropertiesForAllPrivateVariables]`` included. In the future I plan other generators, for example to to provide support for [CommunityToolkit.Mvvm](https://github.com/CommunityToolkit/dotnet) attributes on class level.

# Intention and Usage

I recently had a model class with a lot of variable members and was unnerved by having to type ``public`` all along:

    public class Demo
    {
        public int id;
        public string name;
    }

With this source generator, you can write a class without ``public`` keywords like this:

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

Now you can access the members by their upper-case properties (Note: also C++style m_ and _ prefixes are translated correctly):

        var demoInstance = new Demo();
        demoInstance.Id = 42;
        demoInstance.Name = "Foo";

You should make the ``[GeneratePropertiesForAllPrivateVariables]`` attribute known to your project somewhere, so the code analyzer doesn't put a red zig-zag line under it:

    [AttributeUsage(AttributeTargets.Class/* ToDo: | System.AttributeTargets.Struct */, AllowMultiple = false, Inherited = false)]
    public sealed class GeneratePropertiesForAllPrivateVariablesAttribute : Attribute
    {
    }

# Support for snake_case to CamelCase transformation

Models from APIs often come in the snake_case json form. If you set the attribute argument ``snakeCase2CamelCase`` to ``true``, a snake_case to CamelCase transformation for each private variable member takes place:

    [GeneratePropertiesForAllPrivateVariables(true)]
    public partial class Demo
    {
        string first_name;
        string last_name;
    }

This way you have an easier time adapting the server-side models to C# conformimg CamelCase.

# Limitations

Currently only public non-static classes are supported. It's a bit hacky here and there, but should do the job in 99% of all cases. Consider it a v0.1. Look for 'ToDo', if you want to know more.

If you add some useful features or fix a bug: pull requests welcome!

# License

MIT
