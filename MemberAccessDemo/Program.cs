namespace MemberAccessDemo;

// Copied attribute class from MemberAccess, just to make your code analyzer happy (If you wonder what might be the background, read
/// https://andrewlock.net/creating-a-source-generator-part-7-solving-the-source-generator-marker-attribute-problem-part1/ ):
[AttributeUsage(AttributeTargets.Class/* ToDo: | System.AttributeTargets.Struct */, AllowMultiple = false, Inherited = false)]
public sealed class GeneratePropertiesForAllPrivateVariablesAttribute : Attribute
{
    public GeneratePropertiesForAllPrivateVariablesAttribute(bool snakeCase2CamelCase = false)
    {
    }
}

[GeneratePropertiesForAllPrivateVariables]
public partial class Demo
{
    int id;
    string name;
}

[GeneratePropertiesForAllPrivateVariables(true)]
public partial class DemoWithSnakeCaseMembers
{
    string first_name;
    string last_name;
}

partial class Program
{
    static void Main(string[] args)
    {
        var demoInstance = new Demo();

        demoInstance.Id = 42;

        Console.WriteLine($"GeneratePropertiesForAllPrivateVariables attribute: Private member 'id' accessed through automatically generated public property 'Id': {demoInstance.Id}");
    }
}