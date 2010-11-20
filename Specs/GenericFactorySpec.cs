using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using FactoryMan.Generic;

namespace FactoryMan.Specs {

    public class GenericFactories {
        public static GenericFactories F = new GenericFactories();

        public static Sequence<string> Num   = new Sequence<string>(n => n.ToString());
        public static Sequence<string> Breed = new Sequence<string>(n => "Golden " + n.ToString() + " Retriever");

        public Factory<Dog> Dog = new Factory<Dog>(new {
            Name  = new Func<Dog, object>(d => "Rover #" + Num.Next()),
            Breed = new Func<Dog, object>(d => Breed.Next())
        });

        public Factory<Cat> Cat = new Factory<Cat>(new {
            Name  = "Global Paws",
            Breed = new Func<Cat, object>(c => c.Name + " wonders if cats really have breeds")
        });

        public Factory<DogToy> DogToy = new Factory<DogToy>(new {
            Name = "Kong",
            Dog  = new Func<DogToy, object>(dt => F.Dog.Build())
        });

        // the chaining syntax isn't requires for this example, it's simply a different syntax you may use
        public Factory<DogToy> DogToyWithOverrides =
            new Factory<DogToy>().
                Add("Name", "Kong").
                Add("Dog", (o) => F.Dog.Build(new { Name = "CUSTOMIZE DOG NAME!" }));
    }

    [TestFixture]
    public class GenericFactorySpec {

        GenericFactories f = new GenericFactories();

        [SetUp]
        public void Setup() {
            Factory.GenerateAction = null;
            Factory.GenerateMethod = null;

            // reset sequences
            GenericFactories.Num.Number   = 0;
            GenericFactories.Breed.Number = 0;
        }

        [Test]
        public void HasNameAndTypeFromGeneric() {
            var factory = new Factory<Dog>(){ Name = "Dog" };

            Assert.That(factory.Name,       Is.EqualTo("Dog"));
            Assert.That(factory.ObjectType, Is.EqualTo(typeof(Dog)));
        }

        [Test]
        public void NameCanBeInferredFromType() {
            var factory = new Factory<Dog>();

            Assert.That(factory.Name,       Is.EqualTo("Dog"));
            Assert.That(factory.ObjectType, Is.EqualTo(typeof(Dog)));
        }

        // TODO MOVE TO A SHARED SPEC
        [Test]
        public void HasProperties() {
            var factory = new Factory<Dog>();
            Assert.That(factory.Count, Is.EqualTo(0));

            factory.Add("Name", "Rover");
            Assert.That(factory.Count, Is.EqualTo(1));
            Assert.That(factory["Name"], Is.EqualTo("Rover"));

            factory.Add("Breed", (dog) => "Dynamic dog breed!");
            Assert.That(factory.Count, Is.EqualTo(2));
            Assert.That(factory["Breed"], Is.TypeOf<Func<Dog, object>>());
            Assert.That(factory.Func("Breed").Invoke(null), Is.EqualTo("Dynamic dog breed!"));
        }

        // TODO MOVE TO A SHARED SPEC
        [Test]
        public void CanEnumerateOverAllProperties() {
            List<string> propertyNames = new List<string>();

            var factory = new Factory<Dog>();
            foreach (var prop in factory) propertyNames.Add(prop.Key);
            Assert.IsEmpty(propertyNames);

            factory.Add("Name", "Rover");
            foreach (var prop in factory) propertyNames.Add(prop.Key);
            Assert.IsNotEmpty(propertyNames);
            Assert.That(propertyNames.Count, Is.EqualTo(1));
            Assert.That(propertyNames[0], Is.EqualTo("Name"));
            propertyNames.Clear();

            factory.Add("Breed", (dog) => "Dynamic dog breed!");
            foreach (var prop in factory) propertyNames.Add(prop.Key);
            Assert.IsNotEmpty(propertyNames);
            Assert.That(propertyNames.Count, Is.EqualTo(2));
            Assert.That(propertyNames[0], Is.EqualTo("Name"));
            Assert.That(propertyNames[1], Is.EqualTo("Breed"));
        }

        // This is slightly different than the non-generic version (see the lambda)
        [Test]
        public void CanBuildNewInstance() {
            var factory = new Factory<Dog>();
            var dog = factory.Build() as Dog;
            Assert.Null(dog.Name);
            Assert.Null(dog.Breed);

            factory.Add("Name", "Rover");
            dog = factory.Build() as Dog;
            Assert.NotNull(dog.Name);
            Assert.That(dog.Name, Is.EqualTo("Rover"));
            Assert.Null(dog.Breed);

            factory.Add("Breed", (d) => string.Format("A breed for {0} the dog", d.Name));
            dog = factory.Build() as Dog;
            Assert.NotNull(dog.Name);
            Assert.That(dog.Name, Is.EqualTo("Rover"));
            Assert.NotNull(dog.Breed);
            Assert.That(dog.Breed, Is.EqualTo("A breed for Rover the dog"));
        }

        [Test]
        public void BuildDoesNotSaveInstance() {
            var factory = new Factory<Dog>().
                            Add("Name", "Snoopy").
                            Add("Breed", (d) => string.Format("A breed for {0} the dog", d.Name));

            var dog = factory.Build();
            Assert.That(dog.Name, Is.EqualTo("Snoopy"));
            Assert.That(dog.Breed, Is.EqualTo("A breed for Snoopy the dog"));
            Assert.False(dog.IsSaved);
        }

        [Test]
        public void GenerateCanCallAMethodToSaveInstance() {
            var factory = new Factory<Dog>().
                            Add("Name", "Snoopy").
                            Add("Breed", (d) => string.Format("A breed for {0} the dog", d.Name));

            factory.GenerateAction = (d) => d.Save();

            var dog = factory.Gen(); // <--- alias for Generate()
            Assert.That(dog.Name, Is.EqualTo("Snoopy"));
            Assert.That(dog.Breed, Is.EqualTo("A breed for Snoopy the dog"));
            Assert.True(dog.IsSaved);
        }

        [Test]
        public void GenerateCanCallAnActionToSaveInstance() {
            var factory = new Factory<Dog>().
                            Add("Name", "Snoopy").
                            Add("Breed", (d) => string.Format("A breed for {0} the dog", d.Name));

            factory.InstanceGenerateMethod = "Save";

            var dog = factory.Generate();
            Assert.That(dog.Name, Is.EqualTo("Snoopy"));
            Assert.That(dog.Breed, Is.EqualTo("A breed for Snoopy the dog"));
            Assert.True(dog.IsSaved);
        }

        [Test]
        public void CanAddPropertiesViaAnonymousTypes() {
            var dog = new Factory<Dog>().Add(new {
                Name = "Anonymous Rover",
                Breed = "Neato Breed"
            }).Build();
            Assert.That(dog.Name,  Is.EqualTo("Anonymous Rover"));
            Assert.That(dog.Breed, Is.EqualTo("Neato Breed"));

            dog = new Factory<Dog>(new {
                Name = "Different name", Breed = "Cool Breed"
            }).Build();
            Assert.That(dog.Name, Is.EqualTo("Different name"));
            Assert.That(dog.Breed, Is.EqualTo("Cool Breed"));

            dog = new Factory<Dog>().
                Add(new { Name = "Anonymous Rover" }).
                Add("Breed", (d) => "Dynamic breed for " + d.Name).
                Build();

            Assert.That(dog.Name,  Is.EqualTo("Anonymous Rover"));
            Assert.That(dog.Breed, Is.EqualTo("Dynamic breed for Anonymous Rover"));

            dog = new Factory<Dog>(new {
                Name = "_Anonymous Rover_",
                Breed = new Func<Dog, object>(d => "***Dynamic breed for " + d.Name)
            }).
                Build() as Dog;

            Assert.That(dog.Name,  Is.EqualTo("_Anonymous Rover_"));
            Assert.That(dog.Breed, Is.EqualTo("***Dynamic breed for _Anonymous Rover_"));
        }

        [Test]
        public void CanSetDefaultGenerateActionForAllFactories() {
            var factory = new Factory(typeof(Cat));

            Factory.GenerateAction = c => ((Cat)c).SomeString = "Global Generate Run!";
            Assert.That((factory.Generate() as Cat).SomeString, Is.EqualTo("Global Generate Run!"));

            factory.InstanceGenerateAction = c => ((Cat)c).SomeString = "instance override";
            Assert.That((factory.Generate() as Cat).SomeString, Is.EqualTo("instance override"));
            Assert.That((new Factory(typeof(Cat)).Generate() as Cat).SomeString, Is.EqualTo("Global Generate Run!"));
        }

        [Test]
        public void CanSetDefaultGenerateMethodForAllFactories() {
            var factory = new Factory<Cat>();

            Factory.GenerateMethod = "RunToGenerate";
            Assert.That((factory.Generate()).SomeString, Is.EqualTo("Global generate method ran"));

            factory.InstanceGenerateMethod = "DifferentToGenerate";
            Assert.That((factory.Generate()).SomeString, Is.EqualTo("instance override method"));
            Assert.That((new Factory<Cat>()).Generate().SomeString, Is.EqualTo("Global generate method ran"));
        }

        //public void CanCreateAGroupOfFactories() Creating a Factory[] won't work great with generic Factories!
       
        // TODO split this into multiple specs?  it tests abunchof stuff
        [Test]
        public void CanPassAttributesIntoBuildToOverrideThem() {
            var factory = new Factory<Dog>(new {
                Name  = "Rover",
                Breed = "Golden Retriever"
            });

            var dog = factory.Build();
            Assert.That(dog.Name, Is.EqualTo("Rover"));
            Assert.That(dog.Breed, Is.EqualTo("Golden Retriever"));

            dog = factory.Build(new { Name = "Snoopy" });
            Assert.That(dog.Name, Is.EqualTo("Snoopy"));
            Assert.That(dog.Breed, Is.EqualTo("Golden Retriever"));

            dog = factory.Build(new { Name = "Snoopy", Breed = "Pitbull" });
            Assert.That(dog.Name, Is.EqualTo("Snoopy"));
            Assert.That(dog.Breed, Is.EqualTo("Pitbull"));

            // This is somewhat unrealistic, but let's make sure we support a Func in our overrides.
            // If we store our overrides somewhere and set them programmatically, this isn't *too* unrealistic.
            dog = factory.Build(new { Name = "Dynamic Dog", Breed = new Func<object, object>(d => ((Dog)d).Name + " is a cool dog" )});
            Assert.That(dog.Name, Is.EqualTo("Dynamic Dog"));
            Assert.That(dog.Breed, Is.EqualTo("Dynamic Dog is a cool dog"));

            // make sure that if we set Name = "Foo", it doesn't set name = "Rover" and **THEN** set it to "Foo" ... 
            // this could make some objects get REALLY angry, depending on that the setter does!
            dog = factory.Build();
            Assert.That(dog._NameHasBeenSetTo, Is.EqualTo("Rover"));
            dog.Name = "Another Name";
            Assert.That(dog._NameHasBeenSetTo, Is.EqualTo("RoverAnother Name")); // confirm that it's working ...

            dog = factory.Build(new { Name = "Snoopy" });
            Assert.That(dog._NameHasBeenSetTo, Is.EqualTo("Snoopy"));
        }

        [Test]
        public void CanPassAttributesIntoGenerateToOverrideThem() {
            Factory.GenerateMethod = "Save";
            var factory = new Factory<Dog>(new {
                Name = "Rover",
                Breed = "Golden Retriever"
            });

            var dog = factory.Generate();
            Assert.That(dog.Name, Is.EqualTo("Rover"));
            Assert.That(dog.Breed, Is.EqualTo("Golden Retriever"));

            dog = factory.Generate(new { Name = "Snoopy" });
            Assert.That(dog.Name, Is.EqualTo("Snoopy"));
            Assert.That(dog.Breed, Is.EqualTo("Golden Retriever"));
        }

        [Test]
        public void ExampleOfHowFactoriesMightBeDefined() {
            var cat = f.Cat.Build();
            Assert.That(cat.Name, Is.EqualTo("Global Paws"));
            Assert.That(cat.Breed, Is.EqualTo("Global Paws wonders if cats really have breeds"));

            cat = f.Cat.Build(new { Name = "Paul" });
            Assert.That(cat.Name, Is.EqualTo("Paul"));
            Assert.That(cat.Breed, Is.EqualTo("Paul wonders if cats really have breeds"));
        }

        [Test]
        public void SequenceExample() {
            Assert.That(f.Dog.Build().Name, Is.EqualTo("Rover #1"));
            Assert.That(f.Dog.Build().Name, Is.EqualTo("Rover #2"));
            Assert.That(f.Dog.Build().Name, Is.EqualTo("Rover #3"));

            Assert.That(f.Dog.Build().Breed, Is.EqualTo("Golden 4 Retriever"));
            Assert.That(f.Dog.Build().Breed, Is.EqualTo("Golden 5 Retriever"));

        }

        [Test]
        public void AssociationWithoutOverrides() {
            var toy = f.DogToy.Build();
            Assert.That(toy.Name, Is.EqualTo("Kong"));
            Assert.NotNull(toy.Dog);
            Assert.NotNull(toy.Dog.Name);
            Assert.That(toy.Dog.Name, Is.EqualTo("Rover #1"));
        }

        [Test]
        public void AssociationWithOverrides() {
            var toy = f.DogToyWithOverrides.Build();
            Assert.That(toy.Name, Is.EqualTo("Kong"));
            Assert.NotNull(toy.Dog);
            Assert.NotNull(toy.Dog.Name);
            Assert.That(toy.Dog.Name, Is.EqualTo("CUSTOMIZE DOG NAME!"));
        }
    }
}