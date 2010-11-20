using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FactoryMan.Sequences {

    public class Sequence {
        public int Number = 0;
 
        Func<int, object> _func;

        public Sequence(Func<int, object> func) {
            _func = func;
        }

        public object Next() {
            Number += 1;
            return _func.Invoke(Number);
        }
    }
}
