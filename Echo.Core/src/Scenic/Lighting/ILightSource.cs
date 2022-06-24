namespace Echo.Core.Scenic.Lighting;

public interface ILightSource { }

public interface ILightSource<out T> : ILightSource
{
	T Extract();
}