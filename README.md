#SharpUpdater

This is a class, which can update your c# program, without having an extra program.

This project is based on [BetterCoder's](http://www.youtube.com/watch?v=vcaO2oja4xg) work.<br/>
I added the functionality, to update **multiple** files.


##How to use

The great thing about the project is, it is simply to use.

###Changes in you project
    1. Reference your project to SharpUpdate
    2. Inherit the ISharpUpdatable Interface to your main class
    3. Implement ISharpUpdatable
    4. Create an SharpUpdater object and run DoUpdate()
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

###Upload your porject
1. Create your upload.xml like so
    ```
    <?xml version="1.0" encoding="UTF-8"?>
    <sharpUpdate>
       <update appId="UniqueIDforYourProgram">
            <version>"Version"</version>
            <file>
                <url>"URLtoYourFile"</url>
                <filename>"FileName"</filename>
                <md5>"Md5Checksum"</md5>
            </file>
            <description>"DescriotionForUpdate"</description>
            <launchArgs>"Launch Arguments for your App</launchArgs>
        </update>
    </sharpUpdate>
    ```
    or use my [XMLCreator](https://github.com/V-Modder/XMLCreator)

2. Upload your porgram with all additional files and your update.xml to a webserver.

That's it, that's all folks.
