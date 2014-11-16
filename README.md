SharpUpdater
============

This is a class, which can update your c# program, without having an extra program.

This project is based on [BetterCoder's](http://www.youtube.com/watch?v=vcaO2oja4xg) work.<br/>
I added the functionality, to update **multiple** files.


How to use
----------

The great thing about the project is, it is simply to use.

    1. Reference your project to SharpUpdate
    2. Inherit the ISharpUpdatable Interface to your main class
    3. Implement ISharpUpdatable
    4. Create an SharpUpdater object and run DoUpdate()
    
Example
-------

```
using System;
...
using SharpUpdate;

namespace MainFormNamespace
{
    public class MainFormClass : Form, ISharpUpdatable
    {
        public MainFormClass()
        {
            SharpUpdater su = new SharpUpdater(this);
            su.DoUpdate();
        }
    }
}

```

That's it, that's all folks.
