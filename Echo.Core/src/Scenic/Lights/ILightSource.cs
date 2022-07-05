namespace Echo.Core.Scenic.Lights;

public interface ILightSource { }

public interface ILightSource<out T> : ILightSource
{
	T Extract();
}