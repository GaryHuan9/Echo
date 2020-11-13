namespace ForceRenderer.Terminals
{
	public class Monitor : Terminal.Section
	{
		public Monitor(Terminal terminal) : base(terminal) { }

		public override int Height => 3;

		public override void Update() { }
	}
}