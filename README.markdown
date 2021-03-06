FactoryMan
==========

FactoryMan makes it easy to define factories for .NET classes.  It is intended to be used to help you 
write focused and readable tests, but it can be used for anything.  If you're familiar with fixtures, 
factories are a replacement for fixtures.

For more about the intent of FactoryMan, see [factory_girl][], the library that this is based on.  
Much of the text from this page is taken verbatim from the [factory_girl][] README

Download
--------

    Install-Package FactoryMan

or:

Latest version: 1.0.1.0

[Download .dll][]

[Browse Source][]

Defining factories
------------------

Each factory has a Type and a set of attributes

```cs
using FactoryMan;

var dogFactory = new Factory(typeof(Dog), new {
    Name  = "Snoopy",
    Breed = "Beagle"
});

// Generic Version

using FactoryMan.Generic;

var dogFactory = new Factory<Dog>(new {
    Name  = "Snoopy",
    Breed = "Beagle"
});

// You can manually chain calls to Add() properties instead of passing anonymous objects, if preferred

var dogFactory = new Factory<Dog>().
                                   Add("Name",  "Snoopy").
                                   Add("Breed", "Beagle");
```

It is highly recommended that you have one factory for each class that provides the simplest set of attributes necessary to create an instance of that class. If you’re creating models for ASP.NET MVC, that means that you should only provide attributes that are required through validations and that do not have defaults.

Using factories
---------------

FactoryMan supports different build strategies: Build() and Create(), Properties

```cs    
var factory = new Factory<Dog>(...);

// Returns a Dog instance that's not saved
var user = factory.Build();

// Returns a saved Dog instance
var user = factory.Create();

// Returns a Dictionary<string, object> of properties that could be used to build a Dog instance:
attrs = factory.Properties;
```

No matter which strategy is used, it’s possible to override the defined attributes by passing an anonymous object:

```cs
// Build a instance and override the Name property

var dog = dogFactory.Build();
// dog.Name is "Snoopy"

dog = dogFactory.Build(new { Name = "Rover" });
// dog.Name is "Rover"
```

Create() builds your object using Build() and then either calls a parameterless method or executes some arbitrary logic with your object.  You can use Factory.CreateMethod to specify a method to call on your instance to "Save" it or CreateAction to specify a lambda to run using your instance.  If you need a particular instance to use a unique CreateMethod/Action, you can set factory.InstanceCreateMethod/Action.

```cs
Factory.CreateMethod = "Save";

var dog = dogFactory.Create(); // dog.Save() is called and then the dog is returned

Factory.CreateAction = (d) d.Save(); // this does the same thing using an Action instead of a method name
```
 
Lazy Attributes
---------------

Most factory attributes can be added using static values that are evaluated when the factory is defined, but some attributes (such as associations and other attributes that must be dynamically generated) will need values assigned each time an instance is generated. These "lazy" attributes can be added by passing a lambda instead of a value:

```cs
new Factory<Dog>(new {
  Name  = "Rover",
  Breed = new Func<Dog, object>(dog => "Lazily evaluated breed name for dog: " + dog.Name);
});

// If you use the Add() syntax, you can pass a normal lambda, without using "new Func<,>"

new Factory<Dog>().
  Add("Name",  "Rover").
  Add("Breed", dog => "Lazily evaluated breed name for dog: " + dog.Name);

// And you can use them in combination if you prefer

new Factory<Dog>(new {
    Name = "Rover"
  }).
  Add("Breed", dog => "Lazily evaluated breed name for dog: " + dog.Name);
```

Sequences
---------

Unique values in a specific format (for example, e-mail addresses) can be generated using sequences. Sequences are defined by creating an instance of Sequence, and values in a sequence are generated by calling sequence.Next():

```cs
var email = new Sequence(n => "person" + n.ToString() + "@example.com");

email.Next();
// => "person1@example.com"

email.Next();
// => "person2@example.com"
```

Sequences can be used in lazy attributes:

```cs
class MyFactories {

  public static Sequence Email = new Sequence(n => "person" + n.ToString() + "@example.com");

  public Factory<User> Users = new Factory<User>().
                                    Add("Name", "Bob Smith").
                                    Add("Email", o => Email.Next());

}
```

Generics can be used to specify the Type that your Sequence returns:

```cs
// we put sequences in a different namespace so you can easily specify whether you want to use 
// the generic sequence or the regular one.  As opposed to factories, you don't get much benefit 
// from the generic sequence and I like to use the regular one (with my generic factories).
using FactoryMan.Sequences.Generic;

var email = new Sequence<char[]>(n => string.Format("String with number:{0}", n).ToCharArray());
```

Example Usage
-------------

I'll provide some example code for how to best use FactoryMan at some point.  As opposed to factory_girl, 
which makes all factories available globally, FactoryMan doesn't currently track factories or provide an 
API for getting all factories, etc.  For now, we're requiring the user to manage that.  Once I've had a 
chance to use FactoryMan more, I will probably provide some best practices and add code to help, if possible.

For now, here is some example usage from one of the specs ([GenericFactorySpec.cs](http://github.com/remi/FactoryMan/blob/master/Specs/GenericFactorySpec.cs#L10-36))

```cs
// You don't have to use factories this way.  This is just one way to make your factories available to your tests!

using FactoryMan.Generic;
using FactoryMan.Sequences;

public class Factories {

    public Factories() {
        FactoryMan.Factory.CreateMethod = "Save";
    }

    public object Null = FactoryMan.Factory.Null;

    public static Sequence Username     = new Sequence(i => string.Format("bobsmith{0}", i));
    public static Sequence EmailAddress = new Sequence(i => string.Format("bob.{0}@smith.com", i));

    public Factory<User> User = new Factory<User>().
        Add("Username",  u => Username.Next()).
        Add("Email",     u => EmailAddress.Next()).
        Add("Admin",     false).
        Add("FirstName", "Bob").
        Add("LastName",  "Smith");
}

[TestFixture]
public class UserTest {
    Factories f = Factories.F;

    [Test]
    public void requires_username() {
        Assert.False( f.User.Build(new { Username = f.Null  }).IsValid );
        Assert.False( f.User.Build(new { Username = ""      }).IsValid );
        Assert.True(  f.User.Build(new { Username = "sally" }).IsValid );
    }

    [Test]
    public void requires_unique_email_address() {
        Assert.False( f.User.Create(new { Email = f.Null                }).IsValid );
        Assert.False( f.User.Create(new { Email = ""                    }).IsValid );
        Assert.True(  f.User.Create(new { Email = "sally@smith.com"     }).IsValid );
        Assert.False( f.User.Create(new { Email = "sally@smith.com"     }).IsValid ); // <-- email already taken
        Assert.True(  f.User.Create(new { Email = "different@smith.com" }).IsValid );
    }

}
```

License
-------

FactoryMan is released under the MIT license.

[factory_girl]:  http://github.com/thoughtbot/factory_girl
[Download .dll]: http://github.com/remi/FactoryMan/raw/1.0.1.0/FactoryMan/bin/Release/FactoryMan.dll
[Browse Source]: http://github.com/remi/FactoryMan/tree/1.0.1.0
