using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FactoryMan.Specs {

    public class Dog {
        public string _NameHasBeenSetTo = "";

        string _name;
        public string Name {
            get { return _name; }
            set {
                _NameHasBeenSetTo += value;
                _name = value;
            }
        }

        public string Breed { get; set; }

        bool _saved = false;
        public bool IsSaved { get { return _saved; } }

        public void Save() {
            _saved = true;
        }
    }
}