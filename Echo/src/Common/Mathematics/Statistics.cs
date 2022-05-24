namespace Echo.Common.Mathematics;

public partial struct Statistics
{
	public partial void Report(string label);

	public void ReportConst()
	{
		this.Report("rider");
	}

	public static unsafe partial Statistics Sum(Statistics* source, int length);
}