using Echo.UserInterface.Backend;
using Echo.UserInterface.Core;

using var root = new Window(backend => new EchoUI(backend));
root.Launch();