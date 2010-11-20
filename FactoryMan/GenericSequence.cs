using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FactoryMan.Generic {

    public class Sequence<T> {
        public int Number = 0;

        Func<int, T> _func;

        public Sequence(Func<int, T> func) {
            _func = func;
        }

        public T Next() {
            Number += 1;
            return _func.Invoke(Number);
        }
    }
}