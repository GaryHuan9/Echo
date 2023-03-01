# Getting Started

---

:construction: this page is currently under construction! :construction:

## Welcome
Welcome to Echo documentation! Currently, the documentation is divided into three big sections, each detailing a component that can help you understand and use Echo:
1. If you are just starting and want to get something working, continue reading this [Getting Started](1-getting-started.md) section!
2. If you want to learn more about Echo and how it works internally, and you are already familiar with the basics, head on over to [Core Systems](2-core-systems.md) to see how the code is structured.
3. Finally, to construct beautiful scenes that you can render with Echo, you need the simple [Echo Description Language](3-echo-description-language.md) to communicate your cool ideas to the renderer and let it help you realize them!

## User Interface

If you are simply interested in rendering with Echo, and would like to use a nice graphical dashboard do to so (who doesn't), Echo has a convenient user interface (developed in `Echo.UserInterface`) to help you see your renders. This interface also extremely useful when working with code to visualize any error or performance regression.

You can download the latest version of the user interface in the [Release](https://github.com/GaryHuan9/Echo/releases) tab. Running the universal `Echo.UserInterface.dll` file requires at least [.NET 6](https://dotnet.microsoft.com/en-us/download), or you can directly launch a platform-specific prepackaged version like any other application. The interface window is divided into areas, most of which are empty by default. The first area that you will interact with is the `Scheduler`, located at the bottom of your screen:

![scheduler-ui](https://user-images.githubusercontent.com/22217952/221464949-ae617534-a3e5-4405-be0f-81a600a89942.png)

The `Locate` button opens up a file dialogue for you to find the `.echo` file you want to render. You can learn more about `.echo` files in the [Echo Description Language](3-echo-description-language.md) section. For now, let us select `ext/Scenes/Simple/cornell.echo` by double clicking it, which should immediately schedule and begin a render of Echo's version of the [Cornell Box](https://en.wikipedia.org/wiki/Cornell_box). The `Load` button loads the `.echo` file from disk and the `Schedule` button queues it for rendering, but because `Auto Schedule on File Change` is checked, these actions are automatically performed. You can also change the quality (defined in the `.echo` file) of the rendering by switching `Profile`.

Once you are satisfied with the result, click one of the `Save` buttons in the `Render` area to save your image as a file. Obviously, there are many more things happening with the interface, but this is ultimately an introductory guide, and you are free to play around to explore all its features! 

## Code
If you are not satisfied with only using the interface and want to tinker with the internals of Echo or use it in your project, this next glob of text will get you started!

When you got the `Echo.Core` package all setup (through NuGet, `git`, or however you desire) and ready to begin, copy and execute the following little snippet of code:

```csharp
using Echo.Core.Common.Compute;
using Echo.Core.Common.Packed;
using Echo.Core.Processes;
using Echo.Core.Scenic;

using var device = new Device();

var scene = new CornellBox();
var profile = new StandardPathTracedProfile(scene, 15)
	{ Resolution = new Int2(256, 256) };

var render = ScheduledRender.Create(device, profile);

render.Monitor();
render.texture.Save("render.png");
```

You should see some stuff being printed to your standard output stream, reporting the progress of operations. If it is taking too long, I would highly suggest changing to `Release` mode (if you know how); it could speedup the rendering by more than 10x! After everything settles and the printing has finished, see if you can  congratulations! you have just 