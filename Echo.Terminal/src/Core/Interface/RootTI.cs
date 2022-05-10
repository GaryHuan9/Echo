using CodeHelpers.Packed;
using Echo.Terminal.Core.Display;

namespace Echo.Terminal.Core.Interface;

public class RootTI : ParentTI
{
	Int2 _size;

	public Int2 Size
	{
		get => _size;
		set
		{
			if (_size == value) return;
			_size = value;
			UpdateDomain();
		}
	}

	void UpdateDomain() => Domain = Domain == default ? new Domain(Size) : Domain.Resize(Size);
}