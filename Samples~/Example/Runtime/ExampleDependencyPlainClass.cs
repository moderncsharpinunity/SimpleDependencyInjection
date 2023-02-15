using SimpleDependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Example
{
    public class ExampleDependencyPlainClass
    {
        [Inject]
        private ExampleDependencyNested dependencyNested;

        public void DoSomethingAlsoComplex()
        {
            dependencyNested.DoSomethingSimple();

            Debug.Log("Something complex can happen in plain classes too");
        }
    }
}
