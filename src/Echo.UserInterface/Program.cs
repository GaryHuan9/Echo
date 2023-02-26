using Echo.UserInterface.Backend;
using Echo.UserInterface.Core;

using var root = new ImGuiRoot(backend => new EchoUI(backend));
root.Launch();