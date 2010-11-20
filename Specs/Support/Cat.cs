using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FactoryMan.Specs {

    public class Cat {
        public string Name { get; set; }
        public string Breed { get; set; }

        bool _saved = false;
        public bool IsSaved { get { return _saved; } }

        public void Save() {
            _saved = true;
        }

        public string SomeString = "Default Value";

        public void RunToGenerate() { SomeString = "Global generate method ran"; }
        public void DifferentToGenerate() { SomeString = "instance override method"; }
    }
}