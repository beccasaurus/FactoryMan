using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using FactoryMan;
using FactoryMan.Sequences;

namespace FactoryMan.Specs {

    public class Factories {
        public static Factories F = new Factories();

        public static Sequence Num   = new Sequence(n => n.ToString());
        public static Sequence Breed = new Sequence(n => "Golden " + n.ToString() + " Retriever");

        public Factory Dog = new Factory(typeof(Dog), new {
            Name  = new Func<Dog, object>(d => "Rover #" + Num.Next()),
            Breed = new Func<Dog, object>(d => Breed.Next())
        });

        public Factory Cat = new Factory(typeof(Cat), new {
            Name  = "Global Paws",
            Breed = new Func<Cat, object>(c => c.Name + " wonders if cats really have breeds")
        });

        public Factory DogToy = new Factory(typeof(DogToy), new {
            Name = "Kong",
            Dog  = new Func<DogToy, object>(dt => F.Dog.Build())
        });

        // the chaining syntax isn't requires for this example, it's simply a different syntax you may use
        public Factory DogToyWithOverrides = 
            new Factory(typeof(DogToy)).
                Add("Name", "Kong").
                Add("Dog",  (o) => F.Dog.Build(new { Name = "CUSTOMIZE DOG NAME!" }));
    }

    [TestFixture]
    public class FactorySpec {

        Factories f = new Factories();

        [SetUp]
        public void Setup() {
            Factory.CreateAction = null;
            Factory.CreateMethod = null;
           
            // reset sequences
            Factories.Num.Number   = 0;
            Factories.Breed.Number = 0;
        }

        [Test]
        public void HasNameAndObjectType() {
            var factory = new Factory { Name = "Dog", ObjectType = typeof(Dog) };

            Assert.That(factory.Name,       Is.EqualTo("Dog"));
            Assert.That(factory.ObjectType, Is.EqualTo(typeof(Dog)));
        }

        [Test]
        public void NameCanBeInferredFromType() {
            var factory = new Factory { ObjectType = typeof(Dog) };

            Assert.That(factory.Name,       Is.EqualTo("Dog"));
            Assert.That(factory.ObjectType, Is.EqualTo(typeof(Dog)));
        }

        [Test]
        public void HasProperties() {
            var factory = new Factory { ObjectType = typeof(Dog) };
            Assert.That(factory.Count, Is.EqualTo(0));

            factory.Add("Name", "Rover");
            Assert.That(factory.Count, Is.EqualTo(1));
            Assert.That(factory["Name"], Is.EqualTo("Rover"));

            factory.Add("Breed", (dog) => "Dynamic dog breed!");
            Assert.That(factory.Count, Is.EqualTo(2));
            Assert.That(factory["Breed"], Is.TypeOf<Func<object, object>>());
            Assert.That(factory.Func("Breed").Invoke(null), Is.EqualTo("Dynamic dog breed!"));
        }

        [Test]
        public void CanAccessPropertiesAsDictionary() {
            var factory = new Factory(typeof(Dog), new {
                Name  = "Rover",
                Breed = "Some Breed"
            });

            Assert.That(factory.Properties.Count, Is.EqualTo(2));
            Assert.True(factory.Properties.ContainsKey("Name"));
            Assert.False(factory.Properties.ContainsKey("Rover"));
            Assert.True(factory.Properties.ContainsValue("Rover"));
            Assert.True(factory.Properties.ContainsKey("Breed"));
        }

        [Test]
        public void CanEnumerateOverAllProperties() {
            List<string> propertyNames = new List<string>();

            var factory = new Factory { ObjectType = typeof(Dog) };
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

        [Test]
        public void CanBuildNewInstance() {
            var factory = new Factory(typeof(Dog));
            var dog = factory.Build() as Dog;
            Assert.Null(dog.Name);
            Assert.Null(dog.Breed);

            factory.Add("Name", "Rover");
            dog = factory.Build() as Dog;
            Assert.NotNull(dog.Name);
            Assert.That(dog.Name, Is.EqualTo("Rover"));
            Assert.Null(dog.Breed);

            factory.Add("Breed", (d) => string.Format("A breed for {0} the dog", ((Dog) d).Name));
            dog = factory.Build() as Dog;
            Assert.NotNull(dog.Name);
            Assert.That(dog.Name, Is.EqualTo("Rover"));
            Assert.NotNull(dog.Breed);
            Assert.That(dog.Breed, Is.EqualTo("A breed for Rover the dog"));
        }

        [Test]
        public void BuildDoesNotSaveInstance() {
            var factory = new Factory(typeof(Dog)).
                            Add("Name", "Snoopy").
                            Add("Breed", (d) => string.Format("A breed for {0} the dog", ((Dog) d).Name));
            
            var dog = factory.Build() as Dog;
            Assert.That(dog.Name, Is.EqualTo("Snoopy"));
            Assert.That(dog.Breed, Is.EqualTo("A breed for Snoopy the dog"));
            Assert.False(dog.IsSaved);
        }

        [Test]
        public void CreateCanCallAMethodToSaveInstance() {
            var factory = new Factory(typeof(Dog)).
                            Add("Name", "Snoopy").
                            Add("Breed", (d) => string.Format("A breed for {0} the dog", ((Dog)d).Name));

            factory.InstanceCreateAction = (d) => ((Dog)d).Save();

            var dog = factory.Gen() as Dog; // <--- alias for Create()
            Assert.That(dog.Name, Is.EqualTo("Snoopy"));
            Assert.That(dog.Breed, Is.EqualTo("A breed for Snoopy the dog"));
            Assert.True(dog.IsSaved);
        }

        [Test]
        public void CreateCanCallAnActionToSaveInstance() {
            var factory = new Factory(typeof(Dog)).
                            Add("Name", "Snoopy").
                            Add("Breed", (d) => string.Format("A breed for {0} the dog", ((Dog)d).Name));

            factory.InstanceCreateMethod = "Save";

            var dog = factory.Create() as Dog;
            Assert.That(dog.Name, Is.EqualTo("Snoopy"));
            Assert.That(dog.Breed, Is.EqualTo("A breed for Snoopy the dog"));
            Assert.True(dog.IsSaved);
        }

        [Test]
        public void CanAddPropertiesViaAnonymousTypes() {
            var dog = new Factory(typeof(Dog)).Add(new {
                Name  = "Anonymous Rover",
                Breed = "Neato Breed"
            }).Build() as Dog;
            Assert.That(dog.Name, Is.EqualTo("Anonymous Rover"));
            Assert.That(dog.Breed, Is.EqualTo("Neato Breed"));

            dog = new Factory(typeof(Dog), new {
                Name = "Different name", Breed = "Cool Breed"
            }).Build() as Dog;
            Assert.That(dog.Name, Is.EqualTo("Different name"));
            Assert.That(dog.Breed, Is.EqualTo("Cool Breed"));

            dog = new Factory(typeof(Dog)).
                Add(new { Name = "Anonymous Rover" }).
                Add("Breed", d => "Dynamic breed for " + ((Dog)d).Name).
                Build() as Dog;

            Assert.That(dog.Name, Is.EqualTo("Anonymous Rover"));
            Assert.That(dog.Breed, Is.EqualTo("Dynamic breed for Anonymous Rover"));

            dog = new Factory(typeof(Dog)).
                Add(new { Name = "Anonymous Rover" }).
                Add("Breed", d => "Dynamic breed for " + ((Dog)d).Name).
                Build() as Dog;

            dog = new Factory(typeof(Dog), new {
                Name  = "_Anonymous Rover_",
                Breed = new Func<Dog, object>(d => "***Dynamic breed for " + d.Name)
            }).
                Build() as Dog;

            Assert.That(dog.Name, Is.EqualTo("_Anonymous Rover_"));
            Assert.That(dog.Breed, Is.EqualTo("***Dynamic breed for _Anonymous Rover_"));
        }

        [Test]
        public void CanSetDefaultCreateActionForAllFactories() {
            var factory = new Factory(typeof(Cat));

            Factory.CreateAction = c => ((Cat)c).SomeString = "Global Create Run!";
            Assert.That((factory.Create() as Cat).SomeString, Is.EqualTo("Global Create Run!"));

            factory.InstanceCreateAction = c => ((Cat)c).SomeString = "instance override";
            Assert.That((factory.Create() as Cat).SomeString, Is.EqualTo("instance override"));
            Assert.That((new Factory(typeof(Cat)).Create() as Cat).SomeString, Is.EqualTo("Global Create Run!"));
        }

        [Test]
        public void CanSetDefaultCreateMethodForAllFactories() {
            var factory = new Factory(typeof(Cat));

            Factory.CreateMethod = "RunToCreate";
            Assert.That((factory.Create() as Cat).SomeString, Is.EqualTo("Global generate method ran"));

            factory.InstanceCreateMethod = "DifferentToCreate";
            Assert.That((factory.Create() as Cat).SomeString, Is.EqualTo("instance override method"));
            Assert.That((new Factory(typeof(Cat)).Create() as Cat).SomeString, Is.EqualTo("Global generate method ran"));
        }

        [Test]
        public void CanCreateAGroupOfFactories() {
            Factory.CreateMethod = "Save";

            var factories = new Factory[] {

                Factory.Define(typeof(Dog), new {
                    Name  = "Rover",
                    Breed = "Golden Retriever"
                }),

                Factory.Define(typeof(Cat), new {
                    Name = "Paul"
                }).Add("Breed", c => "Cat breed for " + ((Cat)c).Name)

            };

            Assert.That(factories.Length, Is.EqualTo(2));
            Assert.That(factories.First().Name, Is.EqualTo("Dog"));
            Assert.That(factories.Last().Name, Is.EqualTo("Cat"));

            var dog = factories.First(f => f.Name == "Dog").Create() as Dog;
            Assert.That(dog.Name, Is.EqualTo("Rover"));
            Assert.That(dog.Breed, Is.EqualTo("Golden Retriever"));
            Assert.True(dog.IsSaved);

            var cat = factories.First(f => f.Name == "Cat").Build() as Cat;
            Assert.That(cat.Name, Is.EqualTo("Paul"));
            Assert.That(cat.Breed, Is.EqualTo("Cat breed for Paul"));
            Assert.False(cat.IsSaved);
        }

        // TODO split this into multiple specs?  it tests abunchof stuff
        [Test]
        public void CanPassAttributesIntoBuildToOverrideThem() {
            var factory = new Factory(typeof(Dog), new {
                Name  = "Rover",
                Breed = "Golden Retriever"
            });

            var dog = factory.Build() as Dog;
            Assert.That(dog.Name, Is.EqualTo("Rover"));
            Assert.That(dog.Breed, Is.EqualTo("Golden Retriever"));

            dog = factory.Build(new { Name = "Snoopy" }) as Dog;
            Assert.That(dog.Name, Is.EqualTo("Snoopy"));
            Assert.That(dog.Breed, Is.EqualTo("Golden Retriever"));

            dog = factory.Build(new { Name = "Snoopy", Breed = "Pitbull" }) as Dog;
            Assert.That(dog.Name, Is.EqualTo("Snoopy"));
            Assert.That(dog.Breed, Is.EqualTo("Pitbull"));

            // This is somewhat unrealistic, but let's make sure we support a Func in our overrides.
            // If we store our overrides somewhere and set them programmatically, this isn't *too* unrealistic.
            dog = factory.Build(new { Name = "Dynamic Dog", Breed = new Func<object, object>(d => ((Dog)d).Name + " is a cool dog" )}) as Dog;
            Assert.That(dog.Name, Is.EqualTo("Dynamic Dog"));
            Assert.That(dog.Breed, Is.EqualTo("Dynamic Dog is a cool dog"));

            // make sure that if we set Name = "Foo", it doesn't set name = "Rover" and **THEN** set it to "Foo" ... 
            // this could make some objects get REALLY angry, depending on that the setter does!
            dog = factory.Build() as Dog;
            Assert.That(dog._NameHasBeenSetTo, Is.EqualTo("Rover"));
            dog.Name = "Another Name";
            Assert.That(dog._NameHasBeenSetTo, Is.EqualTo("RoverAnother Name")); // confirm that it's working ...

            dog = factory.Build(new { Name = "Snoopy" }) as Dog;
            Assert.That(dog._NameHasBeenSetTo, Is.EqualTo("Snoopy"));
        }

        [Test]
        public void CanPassAttributesIntoCreateToOverrideThem() {
            Factory.CreateMethod = "Save";
            var factory = new Factory(typeof(Dog), new {
                Name = "Rover",
                Breed = "Golden Retriever"
            });

            var dog = factory.Create() as Dog;
            Assert.That(dog.Name, Is.EqualTo("Rover"));
            Assert.That(dog.Breed, Is.EqualTo("Golden Retriever"));

            dog = factory.Create(new { Name = "Snoopy" }) as Dog;
            Assert.That(dog.Name, Is.EqualTo("Snoopy"));
            Assert.That(dog.Breed, Is.EqualTo("Golden Retriever"));
        }

        [Test]
        public void ExampleOfHowFactoriesMightBeDefined() {
            var cat = f.Cat.Build() as Cat;
            Assert.That(cat.Name, Is.EqualTo("Global Paws"));
            Assert.That(cat.Breed, Is.EqualTo("Global Paws wonders if cats really have breeds"));

            cat = f.Cat.Build(new { Name = "Paul" }) as Cat;
            Assert.That(cat.Name, Is.EqualTo("Paul"));
            Assert.That(cat.Breed, Is.EqualTo("Paul wonders if cats really have breeds"));
        }

        [Test]
        public void SequenceExample() {
            Assert.That((f.Dog.Build() as Dog).Name, Is.EqualTo("Rover #1"));
            Assert.That((f.Dog.Build() as Dog).Name, Is.EqualTo("Rover #2"));
            Assert.That((f.Dog.Build() as Dog).Name, Is.EqualTo("Rover #3"));

            Assert.That((f.Dog.Build() as Dog).Breed, Is.EqualTo("Golden 4 Retriever"));
            Assert.That((f.Dog.Build() as Dog).Breed, Is.EqualTo("Golden 5 Retriever"));
        }

        [Test]
        public void AssociationWithoutOverrides() {
            var toy = f.DogToy.Build() as DogToy;
            Assert.That(toy.Name, Is.EqualTo("Kong"));
            Assert.NotNull(toy.Dog);
            Assert.NotNull(toy.Dog.Name);
            Assert.That(toy.Dog.Name, Is.EqualTo("Rover #1"));
        }

        [Test]
        public void AssociationWithOverrides() {
            var toy = f.DogToyWithOverrides.Build() as DogToy;
            Assert.That(toy.Name, Is.EqualTo("Kong"));
            Assert.NotNull(toy.Dog);
            Assert.NotNull(toy.Dog.Name);
            Assert.That(toy.Dog.Name, Is.EqualTo("CUSTOMIZE DOG NAME!"));
        }

        [Test]
        public void CanSpecifyNullValuesInPropertiesToForcePropertyToBeSetToNull() {
            var dog = f.Dog.Build(new { Name = (object) null, Breed = "A breed" }) as Dog;
            Assert.Null(dog.Name);
            Assert.That(dog.Breed, Is.EqualTo("A breed"));
        }

        [Test]
        public void CanUseFactoryNullIsYouWantForACleanerNullSyntax() {
            var dog = f.Dog.Build(new { Name = Factory.Null, Breed = "A breed" }) as Dog;
            Assert.Null(dog.Name);
            Assert.That(dog.Breed, Is.EqualTo("A breed"));
        }

        // Wishlist:
        //
        // FactoryPropertiesCanBeADictionaryInsteadOfAnObject (?)
        // BuildOverridesCanBeADictionaryInsteadOfAnObject    (?)
    }
}
